#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

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

            CustomCommand cmdParent = new CustomCommand(CommandCategory.Tools, "UltraPasteGroup") { DisplayName = L.UltraPaste };
            CustomCommands.Add(cmdParent);

            CustomCommand cmdDoPaste = new CustomCommand(CommandCategory.Tools, "0_UltraPaste") { DisplayName = L.UltraPaste };
            cmdDoPaste.Invoked += delegate (object o, EventArgs e)
            {
                UltraPasteCommon.DoPaste();
            };
            cmdDoPaste.SetIconFile("UltraPaste.png");
            cmdParent.AddChild(cmdDoPaste);
            CustomCommands.Add(cmdDoPaste);

            CustomCommand cmdWindow = new CustomCommand(CommandCategory.Tools, "0_UltraPasteWindow") { DisplayName = L.UltraPasteWindow };
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

            CustomCommand cmdClipboardImage = new CustomCommand(CommandCategory.Tools, "ClipboardImage") { DisplayName = L.ClipboardImage };
            cmdParent.AddChild(cmdClipboardImage);
            CustomCommands.Add(cmdClipboardImage);

            CustomCommand cmdSaveSnapshotToClipboard = new CustomCommand(CommandCategory.Tools, "SaveSnapshotToClipboard") { DisplayName = L.SaveSnapshotToClipboard };
            cmdSaveSnapshotToClipboard.Invoked += (o, e) => { UltraPasteCommon.Vegas.SaveSnapshot(); };
            cmdSaveSnapshotToClipboard.SetIconFile("UltraPaste.png");
            cmdClipboardImage.AddChild(cmdSaveSnapshotToClipboard);
            CustomCommands.Add(cmdSaveSnapshotToClipboard);

            CustomCommand cmdSaveSnapshotToClipboardAndFile = new CustomCommand(CommandCategory.Tools, "SaveSnapshotToClipboardAndFile") { DisplayName = L.SaveSnapshotToClipboardAndFile };
            cmdSaveSnapshotToClipboardAndFile.Invoked += UltraPasteCommon.SaveSnapshotToClipboardAndFile;
            cmdSaveSnapshotToClipboardAndFile.SetIconFile("UltraPaste.png");
            cmdClipboardImage.AddChild(cmdSaveSnapshotToClipboardAndFile);
            CustomCommands.Add(cmdSaveSnapshotToClipboardAndFile);

            CustomCommand cmdReaperData = new CustomCommand(CommandCategory.Tools, "ReaperData") { DisplayName = L.ReaperData };
            cmdParent.AddChild(cmdReaperData);
            CustomCommands.Add(cmdReaperData);

            CustomCommand cmdExportSelectedEventsToReaperData = new CustomCommand(CommandCategory.Tools, "ExportSelectedEventsToReaperData") { DisplayName = L.ExportSelectedEventsToReaperData };
            cmdExportSelectedEventsToReaperData.Invoked += UltraPasteCommon.ExportSelectedEventsToReaperData;
            cmdExportSelectedEventsToReaperData.SetIconFile("UltraPaste.png");
            cmdReaperData.AddChild(cmdExportSelectedEventsToReaperData);
            CustomCommands.Add(cmdExportSelectedEventsToReaperData);

            CustomCommand cmdExportSelectedTracksToReaperData = new CustomCommand(CommandCategory.Tools, "ExportSelectedTracksToReaperData") { DisplayName = L.ExportSelectedTracksToReaperData };
            cmdExportSelectedTracksToReaperData.Invoked += UltraPasteCommon.ExportSelectedTracksToReaperData;
            cmdExportSelectedTracksToReaperData.SetIconFile("UltraPaste.png");
            cmdReaperData.AddChild(cmdExportSelectedTracksToReaperData);
            CustomCommands.Add(cmdExportSelectedTracksToReaperData);

            CustomCommand cmdPsdImport = new CustomCommand(CommandCategory.Tools, "PsdImport") { DisplayName = L.PsdImport };
            cmdParent.AddChild(cmdPsdImport);
            CustomCommands.Add(cmdPsdImport);

            CustomCommand cmdPsdAddOtherLayers = new CustomCommand(CommandCategory.Tools, "PsdAddOtherLayers") { DisplayName = L.PsdAddOtherLayers };
            cmdPsdAddOtherLayers.Invoked += UltraPasteCommon.PsdAddOtherLayers;
            cmdPsdAddOtherLayers.SetIconFile("UltraPaste.png");
            cmdPsdImport.AddChild(cmdPsdAddOtherLayers);
            CustomCommands.Add(cmdPsdAddOtherLayers);

            CustomCommand cmdSubtitlesImport = new CustomCommand(CommandCategory.Tools, "SubtitlesImport") { DisplayName = L.SubtitlesImport };
            cmdParent.AddChild(cmdSubtitlesImport);
            CustomCommands.Add(cmdSubtitlesImport);

            CustomCommand cmdSubtitlesApplyToSelectedEvents = new CustomCommand(CommandCategory.Tools, "SubtitlesApplyToSelectedEvents") { DisplayName = L.SubtitlesApplyToSelectedEvents };
            cmdSubtitlesApplyToSelectedEvents.Invoked += UltraPasteCommon.SubtitlesApplyToSelectedEvents;
            cmdSubtitlesApplyToSelectedEvents.SetIconFile("UltraPaste.png");
            cmdSubtitlesImport.AddChild(cmdSubtitlesApplyToSelectedEvents);
            CustomCommands.Add(cmdSubtitlesApplyToSelectedEvents);

            CustomCommand cmdSubtitlesTitlesAndTextToProTypeTitler = new CustomCommand(CommandCategory.Tools, "SubtitlesTitlesAndTextToProTypeTitler") { DisplayName = L.SubtitlesTitlesAndTextToProTypeTitler.Replace("&", "&&") };
            cmdSubtitlesTitlesAndTextToProTypeTitler.Invoked += UltraPasteCommon.SubtitlesTitlesAndTextToProTypeTitler;
            cmdSubtitlesTitlesAndTextToProTypeTitler.SetIconFile("UltraPaste.png");
            cmdSubtitlesImport.AddChild(cmdSubtitlesTitlesAndTextToProTypeTitler);
            CustomCommands.Add(cmdSubtitlesTitlesAndTextToProTypeTitler);

            CustomCommand cmdSubtitlesInput = new CustomCommand(CommandCategory.Tools, "0_SubtitlesInput") { DisplayName = L.SubtitlesInputLabel };
            cmdSubtitlesInput.Invoked += delegate (object o, EventArgs e)
            {
                UltraPasteCommon.DoPaste(true);
            };
            cmdSubtitlesTitlesAndTextToProTypeTitler.SetIconFile("UltraPaste.png");
            cmdSubtitlesImport.AddChild(cmdSubtitlesInput);
            CustomCommands.Add(cmdSubtitlesInput);

            CustomCommand cmdSubtitlesInputBox = new CustomCommand(CommandCategory.Tools, "0_SubtitlesInputBox") { DisplayName = L.SubtitlesInputBox };
            cmdSubtitlesInputBox.Invoked += delegate (object o, EventArgs e)
            {
                if (!myVegas.ActivateDockView("UltraPaste_Window_SubtitlesInput"))
                {
                    myVegas.LoadDockView(new UltraPasteWindowSubtitlesInput { AutoLoadCommand = cmdSubtitlesInputBox });
                }
            };
            cmdSubtitlesInputBox.MenuPopup += delegate (object o, EventArgs e)
            {
                cmdSubtitlesInputBox.Checked = myVegas.FindDockView("UltraPaste_Window_SubtitlesInput");
            };
            cmdSubtitlesInputBox.SetIconFile("UltraPaste.png");
            cmdSubtitlesImport.AddChild(cmdSubtitlesInputBox);
            CustomCommands.Add(cmdSubtitlesInputBox);

            CustomCommand cmdMediaImport = new CustomCommand(CommandCategory.Tools, "MediaImport") { DisplayName = L.MediaImport };
            cmdParent.AddChild(cmdMediaImport);
            CustomCommands.Add(cmdMediaImport);

            CustomCommand cmdAddMissingStreams = new CustomCommand(CommandCategory.Tools, "AddMissingStreams") { DisplayName = L.AddMissingStreams };
            cmdAddMissingStreams.Invoked += UltraPasteCommon.MediaAddMissingStreams;
            cmdAddMissingStreams.SetIconFile("UltraPaste.png");
            cmdMediaImport.AddChild(cmdAddMissingStreams);
            CustomCommands.Add(cmdAddMissingStreams);

            CustomCommand cmdVegasData = new CustomCommand(CommandCategory.Tools, "VegasData") { DisplayName = L.VegasData };
            cmdParent.AddChild(cmdVegasData);
            CustomCommands.Add(cmdVegasData);

            CustomCommand cmdGenerateMixedVegasClipboardData = new CustomCommand(CommandCategory.Tools, "GenerateMixedVegasClipboardData") { DisplayName = L.GenerateMixedVegasClipboardData };
            cmdGenerateMixedVegasClipboardData.Invoked += UltraPasteCommon.GenerateMixedVegasClipboardData;
            cmdGenerateMixedVegasClipboardData.SetIconFile("UltraPaste.png");
            cmdVegasData.AddChild(cmdGenerateMixedVegasClipboardData);
            CustomCommands.Add(cmdGenerateMixedVegasClipboardData);

#if TEST
            CustomCommand cmdTest = new CustomCommand(CommandCategory.Tools, "UltraPaste_Test");
            cmdTest.Invoked += delegate (object o, EventArgs e)
            {
                try
                {

                }
                catch (Exception ex){ myVegas.ShowError(ex); }
            };
            cmdTest.SetIconFile("UltraPaste.png");
            cmdParent.AddChild(cmdTest);
            CustomCommands.Add(cmdTest);
#endif
        }
    }
}