#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Collections.Generic;

namespace UltraPaste
{
    internal class UltraPasteCommand
    {

        private Vegas myVegas;

        internal void UltraPasteInit(Vegas vegas, ref List<CustomCommand> CustomCommands)
        {
            myVegas = vegas;
            UltraPasteCommon.Vegas = vegas;
            L.Localize();
            CustomCommand cmdParent = new CustomCommand(CommandCategory.Tools, "UltraPaste_Group") { DisplayName = L.UltraPaste };
            CustomCommands.Add(cmdParent);
            CustomCommand cmdWindow = new CustomCommand(CommandCategory.Tools, "UltraPaste_Window") { DisplayName = L.UltraPasteWindow };
            cmdWindow.Invoked += delegate (object o, EventArgs e)
            {
                if (!myVegas.ActivateDockView("UltraPaste_Window"))
                {
                    myVegas.LoadDockView(new UltraPasteWindow { AutoLoadCommand = cmdWindow });
                }
            };
            cmdWindow.MenuPopup += delegate (object o, EventArgs e)
            {
                cmdWindow.Checked = myVegas.FindDockView("UltraPaste_Window");
            };
            cmdWindow.SetIconFile("UltraPaste.png");
            cmdParent.AddChild(cmdWindow);
            CustomCommands.Add(cmdWindow);
            CustomCommand cmdDoPaste = new CustomCommand(CommandCategory.Tools, "UltraPaste") { DisplayName = L.UltraPaste };
            cmdDoPaste.Invoked += delegate (object o, EventArgs e)
            {
                UltraPasteCommon.DoPaste();
            };
            cmdDoPaste.SetIconFile("UltraPaste.png");
            cmdParent.AddChild(cmdDoPaste);
            CustomCommands.Add(cmdDoPaste);

            CustomCommand cmdAddMissingStreams = new CustomCommand(CommandCategory.Tools, "UltraPaste_AddMissingStreams") { DisplayName = "AddMissingStreams" };
            cmdAddMissingStreams.Invoked += delegate (object o, EventArgs e)
            {
                using (UndoBlock undo = new UndoBlock(myVegas.Project, L.UltraPaste))
                {
                    myVegas.Project.AddMissingStreams(myVegas.Project.GetSelectedEvents<TrackEvent>());
                }
            };
            cmdAddMissingStreams.SetIconFile("UltraPaste.png");
            cmdParent.AddChild(cmdAddMissingStreams);
            CustomCommands.Add(cmdAddMissingStreams);

            CustomCommand cmdUnifyVegasClipboardData = new CustomCommand(CommandCategory.Tools, "UltraPaste_UnifyVegasClipboardData") { DisplayName = "UnifyVegasClipboardData" };
            cmdUnifyVegasClipboardData.Invoked += delegate (object o, EventArgs e)
            {
                Common.UnifyVegasClipboardData();
            };
            cmdUnifyVegasClipboardData.SetIconFile("UltraPaste.png");
            cmdParent.AddChild(cmdUnifyVegasClipboardData);
            CustomCommands.Add(cmdUnifyVegasClipboardData);



#if TEST
            CustomCommand cmdTest = new CustomCommand(CommandCategory.Tools, "UltraPaste_SaveSettings");
            cmdTest.Invoked += delegate (object o, EventArgs e)
            {
                try
                {
                    UltraPasteCommon.Settings.SaveToFile();
                }
                catch (Exception ex)
                {
                    myVegas.ShowError(ex);
                }
            };
            cmdTest.SetIconFile("UltraPaste.png");
            cmdParent.AddChild(cmdTest);
            CustomCommands.Add(cmdTest);

            cmdTest = new CustomCommand(CommandCategory.Tools, "UltraPaste_LoadSettings");
            cmdTest.Invoked += delegate (object o, EventArgs e)
            {
                try
                {
                    UltraPasteCommon.Settings = UltraPasteSettings.LoadFromFile();
                }
                catch (Exception ex)
                {
                    myVegas.ShowError(ex);
                }
            };
            cmdTest.SetIconFile("UltraPaste.png");
            cmdParent.AddChild(cmdTest);
            CustomCommands.Add(cmdTest);
#endif
        }
    }
}