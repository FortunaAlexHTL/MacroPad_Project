// Description: Executes keyboard macros through CoreGraphics and opens macOS
// applications, releasing native keyboard events after they are posted.

namespace MacroPad_application.Controllers;

using System.Runtime.InteropServices;
using System.Diagnostics;
using Models;

public class MacOSController
{
    const ulong CommandFlag = 0x00100000;
    const ulong ShiftFlag = 0x00020000;
    const ulong OptionFlag = 0x00080000;
    const ulong ControlFlag = 0x00040000;

    /// <summary>
    /// Creates a native keyboard event for a key press or release.
    /// </summary>
    /// <param name="source">The event source handle, or IntPtr.Zero to use the default source.</param>
    /// <param name="virtualKey">The macOS virtual keycode of the key.</param>
    /// <param name="keyDown">True for a key press; false for a key release.</param>
    /// <returns>A handle to the created event, or IntPtr.Zero if creation fails.</returns>
    /// <remarks>The caller owns the returned event and must release a nonzero handle with CFRelease.</remarks>
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    static extern IntPtr CGEventCreateKeyboardEvent(
        IntPtr source,
        ushort virtualKey,
        bool keyDown
    );

    /// <summary>
    /// Posts a native event into the macOS event stream.
    /// </summary>
    /// <param name="tap">The location in the event stream where the event is posted.</param>
    /// <param name="event">The handle of the event to post.</param>
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    static extern void CGEventPost(
        uint tap,
        IntPtr @event
    );

    /// <summary>
    /// Sets the flags associated with a native event.
    /// </summary>
    /// <param name="event">The handle of the event to update.</param>
    /// <param name="flags">The combined event flags, including active keyboard modifiers.</param>
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    static extern void CGEventSetFlags(IntPtr @event, ulong flags);

    /// <summary>
    /// Releases an owned native Core Foundation object.
    /// </summary>
    /// <param name="handle">A nonzero handle to the object to release.</param>
    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr handle);

    /// <summary>
    /// Creates, posts, and releases a native keyboard event.
    /// </summary>
    /// <param name="keyCode">The macOS virtual keycode.</param>
    /// <param name="flags">The modifier flags attached to the event.</param>
    /// <param name="isKeyDown">True to press the key; false to release it.</param>
    /// <exception cref="InvalidOperationException">CoreGraphics could not create the event.</exception>
    /// <remarks>
    /// Releases the created event in a finally block, including when processing throws an exception.
    /// Releasing the event frees native memory; a separate key-release event is needed to release the key.
    /// </remarks>
    private void SendKeyEvent(ushort keyCode, ulong flags, bool isKeyDown)
    {
        IntPtr keyboardEvent = CGEventCreateKeyboardEvent(
            IntPtr.Zero, keyCode, isKeyDown);

        if (keyboardEvent == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not create the keyboard event.");
        }

        try
        {
            CGEventSetFlags(keyboardEvent, flags);
            CGEventPost(0, keyboardEvent);
        }
        finally
        {
            CFRelease(keyboardEvent);
        }
    }


    /// <summary>
    /// Creates and posts a key-press event with the supplied modifier flags.
    /// </summary>
    /// <param name="keyCode">The macOS virtual keycode of the key to press.</param>
    /// <param name="flags">The combined modifier flags to attach to the event, or zero for none.</param>
    /// <exception cref="InvalidOperationException">CoreGraphics could not create the event.</exception>
    private void KeyDown(ushort keyCode, ulong flags)
    {
        SendKeyEvent(keyCode, flags, true);
    }

    /// <summary>
    /// Creates and posts a key-release event with the supplied modifier flags.
    /// </summary>
    /// <param name="keyCode">The macOS virtual keycode of the key to release.</param>
    /// <param name="flags">The combined modifier flags to attach to the event, or zero for none.</param>
    /// <exception cref="InvalidOperationException">CoreGraphics could not create the event.</exception>
    private void KeyUp(ushort keyCode, ulong flags)
    {
        SendKeyEvent(keyCode, flags, false);
    }

    /// <summary>
    /// Checks whether a keycode is one of the supported Command, Shift, Option, or Control keys.
    /// </summary>
    /// <param name="keyCode">The macOS virtual keycode to check.</param>
    /// <returns>True for keycodes 54, 55, 56, 60, 58, 61, 59 or 62; otherwise, false.</returns>
    private bool IsModifier(ushort keyCode)
    {
        return keyCode == 55 || // Command
               keyCode == 54 || // Right Command
               keyCode == 56 || // Shift
               keyCode == 60 || // Right Shift
               keyCode == 58 || // Option
               keyCode == 61 || // Right Option
               keyCode == 59 || // Control
               keyCode == 62;   // Right Control
    }

    /// <summary>
    /// Gets the CoreGraphics event flag for a supported modifier key.
    /// </summary>
    /// <param name="keyCode">The macOS virtual keycode of the modifier key.</param>
    /// <returns>The corresponding modifier flag, or zero if the keycode is not supported.</returns>
    private ulong GetModifierFlag(ushort keyCode)
    {
        ulong flag;

        switch (keyCode)
        {
            case 55: case 54:
                flag = CommandFlag;
                break;
            case 56: case 60:
                flag = ShiftFlag;
                break;
            case 58: case 61:
                flag = OptionFlag;
                break;
            case 59: case 62:
                flag = ControlFlag;
                break;
            default:
                flag = 0;
                break;
        }

        return flag;
    }

    /// <summary>Executes the selected keyboard or application action.</summary>
    /// <param name="macro">The macro selected for the device and button.</param>
    /// <remarks>Expected failures are reported by the application loop; partial actions are never replayed.</remarks>
    public void ExecuteMacro(Macro macro)
    {
        if (macro.ActionType == "Application")
        {
            OpenApplication(macro.ApplicationPath);
        }
        else if (macro.ActionType == "Keyboard")
        {
            ExecuteKeyboard(macro.Keys);
        }
        else
        {
            throw new InvalidDataException($"Unsupported action type: {macro.ActionType}.");
        }
    }

    /// <summary>Requests an application launch using macOS open.</summary>
    /// <param name="applicationPath">The configured application path.</param>
    /// <remarks>Arguments are passed separately so spaces and quotes stay part of the path. Does not wait for open to exit.</remarks>
    private void OpenApplication(string? applicationPath)
    {
        if (string.IsNullOrWhiteSpace(applicationPath))
        {
            throw new InvalidDataException("No application path configured.");
        }

        ProcessStartInfo startInfo = new ProcessStartInfo("open");
        startInfo.ArgumentList.Add("-a");
        startInfo.ArgumentList.Add(applicationPath);
        // Disposing this handle does not close the launched application.
        using (Process? process = Process.Start(startInfo))
        {
            if (process == null)
            {
                throw new InvalidOperationException("Could not start macOS open.");
            }
        }
    }

    /// <summary>Converts all keycodes before any native event is posted.</summary>
    /// <param name="keys">Configured numeric keycodes.</param>
    /// <returns>Keycodes that fit the native unsigned 16-bit parameter.</returns>
    /// <remarks>This checks the numeric range, not whether every code represents a supported physical key.</remarks>
    private ushort[] ReadKeyCodes(int[] keys)
    {
        if (keys == null)
        {
            throw new InvalidDataException("Keyboard actions require a Keys array.");
        }

        ushort[] keyCodes = new ushort[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] < ushort.MinValue || keys[i] > ushort.MaxValue)
            {
                throw new InvalidDataException($"Keycode {keys[i]} is outside the allowed numeric range.");
            }
            keyCodes[i] = (ushort)keys[i];
        }
        return keyCodes;
    }

    /// <summary>Presses modifiers, types ordinary keys in order and releases modifiers in reverse order.</summary>
    /// <param name="keys">The configured key sequence.</param>
    /// <remarks>Finally attempts to release keys still marked as pressed when a later event fails.</remarks>
    private void ExecuteKeyboard(int[] keys)
    {
        ushort[] keyCodes = ReadKeyCodes(keys);
        bool[] pressedKeys = new bool[keyCodes.Length];
        ulong modifierFlags = 0;

        try
        {
            for (int i = 0; i < keyCodes.Length; i++)
            {
                if (IsModifier(keyCodes[i]))
                {
                    ulong flagsWithModifier = modifierFlags | GetModifierFlag(keyCodes[i]);
                    KeyDown(keyCodes[i], flagsWithModifier);
                    modifierFlags = flagsWithModifier;
                    pressedKeys[i] = true;
                }
            }

            for (int i = 0; i < keyCodes.Length; i++)
            {
                if (!IsModifier(keyCodes[i]))
                {
                    KeyDown(keyCodes[i], modifierFlags);
                    pressedKeys[i] = true;
                    KeyUp(keyCodes[i], modifierFlags);
                    pressedKeys[i] = false;
                }
            }
        }
        finally
        {
            ReleasePressedKeys(keyCodes, pressedKeys, modifierFlags);
        }
    }

    /// <summary>Attempts every remaining key release even if one release fails.</summary>
    /// <param name="keyCodes">The converted sequence.</param>
    /// <param name="pressedKeys">Which keys were pressed but have not yet been released.</param>
    /// <param name="modifierFlags">The flags active when execution ended.</param>
    private void ReleasePressedKeys(ushort[] keyCodes, bool[] pressedKeys, ulong modifierFlags)
    {
        Logger logger = new Logger();
        for (int i = keyCodes.Length - 1; i >= 0; i--)
        {
            if (pressedKeys[i])
            {
                try
                {
                    KeyUp(keyCodes[i], modifierFlags);
                }
                catch (Exception exception) when (exception is InvalidOperationException || exception is DllNotFoundException
                                                  || exception is EntryPointNotFoundException)
                {
                    logger.LogError($"Could not release key {keyCodes[i]}: {exception.Message}");
                }
                modifierFlags &= ~GetModifierFlag(keyCodes[i]);
            }
        }
    }
}
