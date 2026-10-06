//--------------------------------------//
//     Fortuna Alexandru Cristian       //
//--------------------------------------//
//              18.09.2026              //
//--------------------------------------//
//               MacroPad               //
//--------------------------------------//

namespace MacroPad_application;

using System;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Diagnostics;

class Program
{
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")] //Importing the CoreGraphics framework
    static extern IntPtr CGEventCreateKeyboardEvent(
        IntPtr source,
        ushort virtualKey,
        bool keyDown
    );

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")] //Importing the CoreGraphics framework
    static extern void CGEventPost(
        uint tap,
        IntPtr @event
    );
    
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    static extern void CGEventSetFlags(IntPtr @event, ulong flags);

    const ulong CommandFlag = 0x00100000;
    const ulong ShiftFlag = 0x00020000;
    const ulong OptionFlag = 0x00080000;
    const ulong ControlFlag = 0x00040000;
    private const int MaxLength = 5;
    private const string SettingsFilePath =
        "/Users/fortunaalex/Documents/Development/MacroPad/MacroPad_application/settings.ini";
    private const string AppsFilePath =
        "/Users/fortunaalex/Documents/Development/MacroPad/MacroPad_application/apps.ini";
    // -1 == Empty keycode box!!
    
    static void Main(string[] args)
    {
        SerialPort arduinoMega = new SerialPort("/dev/cu.usbmodem11401", 115200); //Opening the serial port
        arduinoMega.Open();
        int[,] definedMacro = ReadFile(SettingsFilePath); //Reading the macro from the settings file
        
        Console.WriteLine("MacroPad is running!");
        
        
        while (true)
        {
            
            string receivedData = arduinoMega.ReadLine();
            int buttonCode = Convert.ToInt32(receivedData); 

            int[] receivedMacro = MacroController(buttonCode, definedMacro); // Getting the macro from the settings file based on the button pressed
            receivedMacro = GetMacroType(receivedMacro, out bool isApp); // Getting the macro type from the settings file based on the button pressed
            KeyboardController(receivedMacro, isApp); // Actioning the commands
        }
    }

    static int[] MacroController(int buttonCode, int[,] macro) // Finished
    {
        int[] tempReceivedMacro = new int[MaxLength];
        int loopCounter = 0;
        
        for (int i = 0; i < macro.GetLength(0); i++)
        {
            if (buttonCode == Convert.ToInt32(macro[i, 0]))
            {
                for (int q = 1; q < macro.GetLength(1) && macro[i, q] != -1; q++)
                { 
                    tempReceivedMacro[q - 1] = macro[i, q]; 
                    loopCounter++;
                }
            }
        }
        
        int[] receivedMacro = new int[loopCounter];

        for (int i = 0; i < receivedMacro.Length; i++)
        {
            receivedMacro[i] = tempReceivedMacro[i];
        }
        
        return receivedMacro;
    }

    static int[] GetMacroType(int[] macro, out bool isApp) // Finished
    {
        int[] newMacro = new int[macro.Length-1];

        if (macro[0] == 0)
        {
            isApp = false;
        }
        else
        {
            isApp = true;
        }
        
        for (int i = 1; i < macro.Length; i++)
        {
            newMacro[i-1] = macro[i];
        }
        
        return newMacro;
    }

    static void KeyboardController(int[] receivedMacro, bool isApp) // Finished
    {
        if (isApp)
        {
            string appPath = GetAppPath(receivedMacro[0].ToString());
            
            Process.Start("open", appPath);
        }
        else
        {
            ulong modifierFlags = 0;

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
    
    static void KeyDown(ushort keyCode, ulong flags) // Finished
    {
        IntPtr keyDown = CGEventCreateKeyboardEvent(
            IntPtr.Zero, keyCode, true);

        CGEventSetFlags(keyDown, flags);
        CGEventPost(0, keyDown);
    }

    static void KeyUp(ushort keyCode, ulong flags) // Finished
    {
        IntPtr keyUp = CGEventCreateKeyboardEvent(
            IntPtr.Zero, keyCode, false);

        CGEventSetFlags(keyUp, flags);
        CGEventPost(0, keyUp);
    }
    
    static bool IsModifier(ushort keyCode) // Finished
    {
        return keyCode == 55 ||  // Command
               keyCode == 56 ||  // Shift
               keyCode == 58 ||  // Option
               keyCode == 59;    // Control
    }
    
    static ulong GetModifierFlag(ushort keyCode) // Finished
    {
        switch (keyCode)
        {
            case 55:
                return CommandFlag;

            case 56:
                return ShiftFlag;

            case 58:
                return OptionFlag;

            case 59:
                return ControlFlag;

            default:
                return 0;
        }
    }

    static string GetAppPath(string appIdentifier) // Finished
    {
        string[] fileContent = File.ReadAllLines(AppsFilePath);
        string appPath = "";
        
        for (int i = 0; i < fileContent.Length; i++)
        {
            string[] app = fileContent[i].Split(";");
            
            if (app[0] == appIdentifier)
            {
                appPath = app[1];
            }
        }
        
        return appPath;
    }
    
    static int[,] ReadFile(string path) // Finished
    {
        string[] fileContents = File.ReadAllLines(path);
        int[,] tempMacro = new int[fileContents.Length, MaxLength + 1];

        for (int i = 0; i < fileContents.Length; i++)
        {
            string[] macroContents = fileContents[i].Split(";");

            for (int q = 0; q < macroContents.Length; q++)
            {
                tempMacro[i, q] = int.Parse(macroContents[q]);
            }

        }
        
        return tempMacro;
    }
}

