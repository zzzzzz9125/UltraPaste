#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Collections.Generic;

namespace UltraPaste
{
    using Utilities;

    internal class UltraPasteCommand
    {
        private Vegas myVegas;

        internal void UltraPasteInit(Vegas vegas, ref List<CustomCommand> CustomCommands)
        {
            myVegas = vegas;
            UltraPasteCommon.Vegas = vegas;
            I18n.Localize();

            CustomCommand cmdParent = new CustomCommand(CommandCategory.Tools, "UltraPasteGroup") { DisplayName = I18n.Translation.UltraPaste };
            CustomCommands.Add(cmdParent);

            CustomCommand cmdDoPaste = new CustomCommand(CommandCategory.Tools, "0_UltraPaste") { DisplayName = I18n.Translation.UltraPaste };
            cmdDoPaste.Invoked += delegate (object o, EventArgs e)
            {
                UltraPasteCommon.DoPaste();
            };
            cmdDoPaste.SetIconFile("UltraPaste.png");
            cmdParent.AddChild(cmdDoPaste);
            CustomCommands.Add(cmdDoPaste);

            CustomCommand cmdWindow = new CustomCommand(CommandCategory.Tools, "0_UltraWindow_Main") { DisplayName = I18n.Translation.UltraPasteWindow };
            cmdWindow.Invoked += delegate (object o, EventArgs e)
            {
                if (!myVegas.ActivateDockView("UltraWindow_Main"))
                {
                    myVegas.LoadDockView(new UltraWindow_Main { AutoLoadCommand = cmdWindow });
                }
            };
            cmdWindow.MenuPopup += delegate (object o, EventArgs e)
            {
                cmdWindow.Checked = myVegas.FindDockView("UltraWindow_Main");
            };
            cmdWindow.SetIconFile("UltraPaste.png");
            cmdParent.AddChild(cmdWindow);
            CustomCommands.Add(cmdWindow);

            CustomCommand cmdClipboardImage = new CustomCommand(CommandCategory.Tools, "ClipboardImage") { DisplayName = I18n.Translation.ClipboardImage };
            cmdParent.AddChild(cmdClipboardImage);
            CustomCommands.Add(cmdClipboardImage);

            CustomCommand cmdSaveSnapshotToClipboard = new CustomCommand(CommandCategory.Tools, "SaveSnapshotToClipboard") { DisplayName = I18n.Translation.SaveSnapshotToClipboard };
            cmdSaveSnapshotToClipboard.Invoked += (o, e) => { UltraPasteCommon.Vegas.SaveSnapshot(); };
            cmdSaveSnapshotToClipboard.SetIconFile("UltraPaste.png");
            cmdClipboardImage.AddChild(cmdSaveSnapshotToClipboard);
            CustomCommands.Add(cmdSaveSnapshotToClipboard);

            CustomCommand cmdSaveSnapshotToClipboardAndFile = new CustomCommand(CommandCategory.Tools, "SaveSnapshotToClipboardAndFile") { DisplayName = I18n.Translation.SaveSnapshotToClipboardAndFile };
            cmdSaveSnapshotToClipboardAndFile.Invoked += UltraPasteCommon.SaveSnapshotToClipboardAndFile;
            cmdSaveSnapshotToClipboardAndFile.SetIconFile("UltraPaste.png");
            cmdClipboardImage.AddChild(cmdSaveSnapshotToClipboardAndFile);
            CustomCommands.Add(cmdSaveSnapshotToClipboardAndFile);

            CustomCommand cmdReaperData = new CustomCommand(CommandCategory.Tools, "ReaperData") { DisplayName = I18n.Translation.ReaperData };
            cmdParent.AddChild(cmdReaperData);
            CustomCommands.Add(cmdReaperData);

            CustomCommand cmdExportSelectedEventsToReaperData = new CustomCommand(CommandCategory.Tools, "ExportSelectedEventsToReaperData") { DisplayName = I18n.Translation.ExportSelectedEventsToReaperData };
            cmdExportSelectedEventsToReaperData.Invoked += UltraPasteCommon.ExportSelectedEventsToReaperData;
            cmdExportSelectedEventsToReaperData.SetIconFile("UltraPaste.png");
            cmdReaperData.AddChild(cmdExportSelectedEventsToReaperData);
            CustomCommands.Add(cmdExportSelectedEventsToReaperData);

            CustomCommand cmdExportSelectedTracksToReaperData = new CustomCommand(CommandCategory.Tools, "ExportSelectedTracksToReaperData") { DisplayName = I18n.Translation.ExportSelectedTracksToReaperData };
            cmdExportSelectedTracksToReaperData.Invoked += UltraPasteCommon.ExportSelectedTracksToReaperData;
            cmdExportSelectedTracksToReaperData.SetIconFile("UltraPaste.png");
            cmdReaperData.AddChild(cmdExportSelectedTracksToReaperData);
            CustomCommands.Add(cmdExportSelectedTracksToReaperData);

            CustomCommand cmdPsdImport = new CustomCommand(CommandCategory.Tools, "PsdImport") { DisplayName = I18n.Translation.PsdImport };
            cmdParent.AddChild(cmdPsdImport);
            CustomCommands.Add(cmdPsdImport);

            CustomCommand cmdPsdAddOtherLayers = new CustomCommand(CommandCategory.Tools, "PsdAddOtherLayers") { DisplayName = I18n.Translation.PsdAddOtherLayers };
            cmdPsdAddOtherLayers.Invoked += UltraPasteCommon.PsdAddOtherLayers;
            cmdPsdAddOtherLayers.SetIconFile("UltraPaste.png");
            cmdPsdImport.AddChild(cmdPsdAddOtherLayers);
            CustomCommands.Add(cmdPsdAddOtherLayers);

            CustomCommand cmdSubtitlesImport = new CustomCommand(CommandCategory.Tools, "SubtitlesImport") { DisplayName = I18n.Translation.SubtitlesImport };
            cmdParent.AddChild(cmdSubtitlesImport);
            CustomCommands.Add(cmdSubtitlesImport);

            CustomCommand cmdSubtitlesApplyToSelectedEvents = new CustomCommand(CommandCategory.Tools, "SubtitlesApplyToSelectedEvents") { DisplayName = I18n.Translation.SubtitlesApplyToSelectedEvents };
            cmdSubtitlesApplyToSelectedEvents.Invoked += UltraPasteCommon.SubtitlesApplyToSelectedEvents;
            cmdSubtitlesApplyToSelectedEvents.SetIconFile("UltraPaste.png");
            cmdSubtitlesImport.AddChild(cmdSubtitlesApplyToSelectedEvents);
            CustomCommands.Add(cmdSubtitlesApplyToSelectedEvents);

            CustomCommand cmdSubtitlesTitlesAndTextToProTypeTitler = new CustomCommand(CommandCategory.Tools, "SubtitlesTitlesAndTextToProTypeTitler") { DisplayName = I18n.Translation.SubtitlesTitlesAndTextToProTypeTitler.Replace("&", "&&") };
            cmdSubtitlesTitlesAndTextToProTypeTitler.Invoked += UltraPasteCommon.SubtitlesTitlesAndTextToProTypeTitler;
            cmdSubtitlesTitlesAndTextToProTypeTitler.SetIconFile("UltraPaste.png");
            cmdSubtitlesImport.AddChild(cmdSubtitlesTitlesAndTextToProTypeTitler);
            CustomCommands.Add(cmdSubtitlesTitlesAndTextToProTypeTitler);

            CustomCommand cmdSubtitlesInput = new CustomCommand(CommandCategory.Tools, "0_SubtitlesInput") { DisplayName = I18n.Translation.SubtitlesInputLabel };
            cmdSubtitlesInput.Invoked += delegate (object o, EventArgs e)
            {
                UltraPasteCommon.DoPaste(true);
            };
            cmdSubtitlesTitlesAndTextToProTypeTitler.SetIconFile("UltraPaste.png");
            cmdSubtitlesImport.AddChild(cmdSubtitlesInput);
            CustomCommands.Add(cmdSubtitlesInput);

            CustomCommand cmdSubtitlesInputBox = new CustomCommand(CommandCategory.Tools, "0_SubtitlesInputBox") { DisplayName = I18n.Translation.SubtitlesInputBox };
            cmdSubtitlesInputBox.Invoked += delegate (object o, EventArgs e)
            {
                if (!myVegas.ActivateDockView("UltraWindow_SubtitlesInput"))
                {
                    myVegas.LoadDockView(new UltraWindow_SubtitlesInput { AutoLoadCommand = cmdSubtitlesInputBox });
                }
            };
            cmdSubtitlesInputBox.MenuPopup += delegate (object o, EventArgs e)
            {
                cmdSubtitlesInputBox.Checked = myVegas.FindDockView("UltraWindow_SubtitlesInput");
            };
            cmdSubtitlesInputBox.SetIconFile("UltraPaste.png");
            cmdSubtitlesImport.AddChild(cmdSubtitlesInputBox);
            CustomCommands.Add(cmdSubtitlesInputBox);

            CustomCommand cmdMediaImport = new CustomCommand(CommandCategory.Tools, "MediaImport") { DisplayName = I18n.Translation.MediaImport };
            cmdParent.AddChild(cmdMediaImport);
            CustomCommands.Add(cmdMediaImport);

            CustomCommand cmdAddMissingStreams = new CustomCommand(CommandCategory.Tools, "AddMissingStreams") { DisplayName = I18n.Translation.AddMissingStreams };
            cmdAddMissingStreams.Invoked += UltraPasteCommon.MediaAddMissingStreams;
            cmdAddMissingStreams.SetIconFile("UltraPaste.png");
            cmdMediaImport.AddChild(cmdAddMissingStreams);
            CustomCommands.Add(cmdAddMissingStreams);

            CustomCommand cmdVegasData = new CustomCommand(CommandCategory.Tools, "VegasData") { DisplayName = I18n.Translation.VegasData };
            cmdParent.AddChild(cmdVegasData);
            CustomCommands.Add(cmdVegasData);

            CustomCommand cmdGenerateMixedVegasClipboardData = new CustomCommand(CommandCategory.Tools, "GenerateMixedVegasClipboardData") { DisplayName = I18n.Translation.GenerateMixedVegasClipboardData };
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