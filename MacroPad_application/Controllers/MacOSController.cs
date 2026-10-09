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
        
    /// <summary>
    /// Executes a keyboard macro or requests an application launch according to its action type.
    /// </summary>
    /// <param name="macro">
    /// The selected macro. Keyboard actions require a non-null key array with values that fit
    /// in ushort; application actions require a nonblank application path.
    /// </param>
    /// <remarks>
    /// Recognizes the exact action types "Keyboard" and "Application"; other types perform no action.
    /// Keyboard actions press modifiers first, send ordinary keys in order, then release modifiers
    /// in reverse order. Application actions invoke macOS open and report a blank path to the console,
    /// but do not check the launch result. Errors propagate to the caller; releasing held modifiers
    /// after an execution error is not guaranteed.
    /// </remarks>
    /// <exception cref="OverflowException">A keyboard code is outside the ushort range.</exception>
    /// <exception cref="InvalidOperationException">CoreGraphics could not create a keyboard event.</exception>
    public void ExecuteMacro(Macro macro)
    {
        ulong modifierFlags = 0;
        if (macro.ActionType == "Application")
        {
            if (!string.IsNullOrWhiteSpace(macro.ApplicationPath))
            {
                Process.Start("open", $"-a \"{macro.ApplicationPath}\"");
            }
            else
            {
                Console.WriteLine("No application path configured.");
            }
        }
        else if (macro.ActionType == "Keyboard")
        {
            int[] receivedMacro = macro.Keys;
            
            for (int i = 0; i < receivedMacro.Length; i++)
            {
                ushort keyCode = Convert.ToUInt16(receivedMacro[i]);
                if (IsModifier(keyCode))
                {
                    modifierFlags |= GetModifierFlag(keyCode);
                    KeyDown(keyCode, modifierFlags);
                }
            }

            for (int i = 0; i < receivedMacro.Length; i++)
            {
                ushort keyCode = Convert.ToUInt16(receivedMacro[i]);
                if (!IsModifier(keyCode))
                {
                    KeyDown(keyCode, modifierFlags);
                    KeyUp(keyCode, modifierFlags);
                }
            }

            for (int i = receivedMacro.Length - 1; i >= 0; i--)
            {
                ushort keyCode = Convert.ToUInt16(receivedMacro[i]);
                if (IsModifier(keyCode))
                {
                    KeyUp(keyCode, modifierFlags);
                    modifierFlags &= ~GetModifierFlag(keyCode);
                }
            }
        }
    }
}
