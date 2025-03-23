﻿#if !Sony
using ScriptPortal.Vegas;
using ScriptPortal.MediaSoftware.TextGen.CoreGraphics;
using ScriptPortal.MediaSoftware.TextGen.CoreGraphics.NodeLibrary;
using ScriptPortal.MediaSoftware.TextGen.CoreGraphics.FilterLibrary;
using ScriptPortal.MediaSoftware.TextGen.CoreGraphics.NodeLibrary.MetaText;
#else
using Sony.Vegas;
using Sony.MediaSoftware.TextGen.CoreGraphics;
using Sony.MediaSoftware.TextGen.CoreGraphics.NodeLibrary;
using Sony.MediaSoftware.TextGen.CoreGraphics.FilterLibrary;
using Sony.MediaSoftware.TextGen.CoreGraphics.NodeLibrary.MetaText;
#endif

using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UltraPaste
{
    public static class TextMediaGenerator
    {
        public const string UID_TITLES_AND_TEXT = "{Svfx:com.vegascreativesoftware:titlesandtext}";
        public const string UID_TITLES_AND_TEXT_SONY = "{Svfx:com.sonycreativesoftware:titlesandtext}";
        public const string UID_PROTYPE_TITLER = "{53FC0B44-BD58-4716-A90F-3EB43168DE81}";
        public const string UID_LEGACY_TEXT = "{0FE8789D-0C47-442A-AFB0-0DAF97669317}";
        public const string UID_TEXT_OFX = "{Svfx:no.openfx.Text}";
        public const string UID_SOLID_COLOR = "{Svfx:com.vegascreativesoftware:solidcolor}";
        public const string UID_SOLID_COLOR_SONY = "{Svfx:com.sonycreativesoftware:solidcolor}";
        public const string UID_IGNITE_PRO_TEXT = "{Svfx:com.FXHOME.HitFilm.Text}";
        public const string UID_IGNITE_PRO_TEXT_360 = "{Svfx:com.FXHOME.HitFilm.360Text}";
        public const string UID_UNIVERSE_TEXT_TYPOGRAPHIC = "{Svfx:com.redgiantsoftware.Universe_Text_Typographic_OFX}";
        public const string UID_UNIVERSE_TEXT_HACKER = "{Svfx:com.redgiantsoftware.Universe_Text_Hacker_Text_OFX}";
        public const string UID_OFX_CLOCK = "{Svfx:de.hlinke.ofxclock}";
        public static PlugInNode PlugInTitlesAndText = UltraPasteCommon.Vegas?.Generators.FindChildByUniqueID(UID_TITLES_AND_TEXT) ?? UltraPasteCommon.Vegas.Generators.FindChildByUniqueID(UID_TITLES_AND_TEXT_SONY);
        public static PlugInNode PlugInProTypeTitler = UltraPasteCommon.Vegas?.Generators.FindChildByUniqueID(UID_PROTYPE_TITLER);
        public static PlugInNode PlugInLegacyText = UltraPasteCommon.Vegas?.Generators.FindChildByUniqueID(UID_LEGACY_TEXT);
        public static PlugInNode PlugInTextOfx = UltraPasteCommon.Vegas?.Generators.FindChildByUniqueID(UID_TEXT_OFX);
        public static PlugInNode PlugInSolidColor = UltraPasteCommon.Vegas?.Generators.FindChildByUniqueID(UID_SOLID_COLOR) ?? UltraPasteCommon.Vegas.Generators.FindChildByUniqueID(UID_SOLID_COLOR_SONY);
        public static PlugInNode PlugInIgniteProText = UltraPasteCommon.Vegas?.VideoFX.FindChildByUniqueID(UID_IGNITE_PRO_TEXT);
        public static PlugInNode PlugInIgniteProText360 = UltraPasteCommon.Vegas?.VideoFX.FindChildByUniqueID(UID_IGNITE_PRO_TEXT_360);
        public static PlugInNode PlugInUniverseTextTypographic = UltraPasteCommon.Vegas?.Generators.FindChildByUniqueID(UID_UNIVERSE_TEXT_TYPOGRAPHIC);
        public static PlugInNode PlugInUniverseTextHacker = UltraPasteCommon.Vegas?.VideoFX.FindChildByUniqueID(UID_UNIVERSE_TEXT_HACKER);
        public static PlugInNode PlugInOfxClock = UltraPasteCommon.Vegas?.Generators.FindChildByUniqueID(UID_OFX_CLOCK);
        public static PlugInNode[] TextPlugIns = new PlugInNode[] { PlugInTitlesAndText, PlugInProTypeTitler, PlugInLegacyText, PlugInTextOfx, PlugInIgniteProText, PlugInIgniteProText360, PlugInUniverseTextTypographic, PlugInUniverseTextHacker, PlugInOfxClock };
        public static Dictionary<int, string> ValidTextNumbersAndNames
        {
            get
            {
                Dictionary<int, string> dic = new Dictionary<int, string>();
                for (int i = 0; i < TextPlugIns.Length; i++)
                {
                    if (TextPlugIns[i] != null)
                    {
                        dic.Add(i, TextPlugIns[i].Name);
                    }
                }
                return dic;
            }
        }

        public static bool IsGenerator(this PlugInNode p)
        {
            if (p == null)
            {
                return false;
            }
            return UltraPasteCommon.Vegas?.Generators.FindChildByUniqueID(p.UniqueID) != null;
        }

        public delegate void TitlerChanger(Titler titler);

        public class TextMediaProperties
        {
            public Size MediaSize = new Size(UltraPasteCommon.Vegas.Project.Video.Width, UltraPasteCommon.Vegas.Project.Video.Height);
            public double MediaSeconds = UltraPasteCommon.Settings.SubtitlesImport.DefaultLengthSeconds;
            public string Text = "";
            public string FontName = "Arial";
            public double FontSize = 48;
            public bool FontItalic = false;
            public bool FontBold = false;
            public int TextAlign = 0;
            public Color TextColor = Color.White;
            public Color OutlineColor = Color.Black;
            public double OutlineWidth = 0;
            public double LocationX = 0.5;
            public double LocationY = 0.5;
            public double ScaleX = 1;
            public double ScaleY = 1;
            public int CenterType = 4;
            public double Tracking = 0;
            public double LineSpacing = 1;
            public bool ShadowEnable = false;
            public Color ShadowColor = Color.Black;
            public double ShadowX = 0.2;
            public double ShadowY = 0.2;
            public double ShadowBlur = 0.4;

            public static TextMediaProperties GetFromTitlesAndText(OFXEffect ofx)
            {
                RichTextBox rtb = new RichTextBox() { Rtf = ((OFXStringParameter)ofx["Text"]).Value };
                OFXDouble2D location = ((OFXDouble2DParameter)ofx["Location"]).Value;
                double scale = ((OFXDoubleParameter)ofx["Scale"]).Value;

                return new TextMediaProperties()
                {
                    Text = rtb.Text,
                    FontName = rtb.SelectionFont.Name,
                    FontSize = rtb.SelectionFont.Size,
                    FontItalic = rtb.SelectionFont.Italic,
                    FontBold = rtb.SelectionFont.Bold,
                    TextAlign = (int)rtb.SelectionAlignment,
                    TextColor = ((OFXRGBAParameter)ofx["TextColor"]).Value.ConvertToColor(),
                    OutlineColor = ((OFXRGBAParameter)ofx["OutlineColor"]).Value.ConvertToColor(),
                    OutlineWidth = ((OFXDoubleParameter)ofx["OutlineWidth"]).Value,
                    LocationX = location.X,
                    LocationY = location.Y,
                    ScaleX = scale,
                    ScaleY = scale,
                    CenterType = ((OFXChoiceParameter)ofx["Alignment"]).Value.Index,
                    Tracking = ((OFXDoubleParameter)ofx["Tracking"]).Value,
                    LineSpacing = ((OFXDoubleParameter)ofx["LineSpacing"]).Value,
                    ShadowEnable = ((OFXBooleanParameter)ofx["ShadowEnable"]).Value,
                    ShadowColor = ((OFXRGBAParameter)ofx["ShadowColor"]).Value.ConvertToColor(),
                    ShadowX = ((OFXDoubleParameter)ofx["ShadowOffsetX"]).Value,
                    ShadowY = ((OFXDoubleParameter)ofx["ShadowOffsetY"]).Value,
                    ShadowBlur = ((OFXDoubleParameter)ofx["ShadowBlur"]).Value
                };
            }

            public Titler GetTitler(string preset = null)
            {
                Titler titler = new Titler(GetMetaTextNode());

                string presetXml = Encoding.UTF8.GetString(PlugInProTypeTitler.LoadDxtEffectPreset(preset));

                if (string.IsNullOrEmpty(presetXml))
                {
                    return titler;
                }

                Titler titlerPreset = presetXml.DeserializeXml<Titler>();
                if (titlerPreset == null)
                {
                    return titler;
                }
                titler = titlerPreset;

                if (titler.MetaTextNodes.Count == 0)
                {
                    titler.MetaTextNodes.Add(GetMetaTextNode());
                }
                else if (titler.MetaTextNodes[0] == null)
                {
                    titler.MetaTextNodes[0] = GetMetaTextNode();
                }
                else if (titler.MetaTextNodes[0].TextSource.Spans.Count == 0)
                {
                    titler.MetaTextNodes[0].TextSource.Spans.Add(GetMetaTextSpan());
                }
                else if (titler.MetaTextNodes[0].TextSource.Spans[0] == null)
                {
                    titler.MetaTextNodes[0].TextSource.Spans[0] = GetMetaTextSpan();
                }
                else
                {
                    titler.MetaTextNodes[0].TextSource.Spans[0].Text = Text;
                }

                return titler;
            }

            public MetaTextNode GetMetaTextNode()
            {
                MetaTextNode node = new MetaTextNode(new MetaTextSource(GetMetaTextSpan()))
                {
                    Tracking = this.Tracking,
                    LineSpacingScaler = LineSpacing,
                    EdTransform = new EditableMatrix((EditableMatrix.CenterPosition)(CenterType > 5 ? 9 : (CenterType + (CenterType < 3 ? 6 : 0))))
                    {
                        Offset = new EditablePoint((LocationX - 0.5) * 320 / 9, (LocationY - 0.5) * 20),
                        CustomCenter = new EditablePoint((CenterType % 3) / 2.0, (CenterType / 3 % 3) / 2.0),
                        Scale = new EditablePoint(ScaleX, ScaleY)
                    },
                    ImageEffects = new List<StandardFilter>()
                {
                    new DropShadow(ShadowEnable)
                    {
                        HorizontalOffset = ShadowX / 10,
                        VerticalOffset = ShadowY / 10,
                        Amount = ShadowBlur / 5,
                        Color = XmlColorFloat.FromColor(ShadowColor)
                    }
                }
                };
                return node;
            }

            public MetaTextSpan GetMetaTextSpan()
            {
                MetaTextSpanProperties spanProperties = new MetaTextSpanProperties
                (
                    FontName,
                    FontItalic ? MetaTextSpanProperties.FontStyleEnum.Italic : MetaTextSpanProperties.FontStyleEnum.Normal,
                    FontBold ? MetaTextSpanProperties.FontWeightEnum.Bold : MetaTextSpanProperties.FontWeightEnum.Normal,
                    FontSize / 10
                )
                {
                    LineAlignment = (MetaTextSpanProperties.Alignment)TextAlign,
                    Style = new ShapeNodeStyle
                    (
                        TextColor,
                        OutlineColor,
                        (float)OutlineWidth,
                        ShapeNodeStyle.StrokeOrderEnum.Under
                    )
                };
                MetaTextSpan span = new MetaTextSpan(Text, spanProperties);
                return span;
            }

            public Media GenerateProTypeTitlerMedia(string presetName = null, TitlerChanger changer = null)
            {
                Titler titler = GetTitler(presetName);
                changer?.Invoke(titler);
                string tmpPresetName = string.Format("{0}_Temp", presetName);
                PlugInProTypeTitler.SaveDxtEffectPresetXml(tmpPresetName, titler.SerializeXml());
                Media myMedia = Media.CreateInstance(UltraPasteCommon.Vegas.Project, PlugInProTypeTitler);
                myMedia.Generator.Preset = tmpPresetName;
                PlugInProTypeTitler.DeleteDxtEffectPreset(tmpPresetName);
                myMedia.GetVideoStreamByIndex(0).Size = MediaSize;
                if (MediaSeconds > 0)
                {
                    myMedia.Length = Timecode.FromSeconds(MediaSeconds);
                }
                return myMedia;
            }
        }

        public static List<VideoEvent> GenerateTextEvents(Timecode start, Timecode length = null, string text = null, int type = 0, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1)
        {
            if (type > TextPlugIns.Length - 1)
            {
                return new List<VideoEvent>();
            }
            PlugInNode plug = TextPlugIns[type];
            if (plug.IsOFX)
            {
                return GenerateOfxEvents(plug, start, length, text, presetName, useMultipleSelectedTracks, newTrackIndex);
            }
            else
            {
                return plug.UniqueID == PlugInProTypeTitler.UniqueID ? GenerateProTypeTitlerEvents(start, length, text, presetName, useMultipleSelectedTracks, newTrackIndex)
                     : plug.UniqueID == PlugInLegacyText.UniqueID    ? GenerateLegacyTextEvents   (start, length, text, presetName, useMultipleSelectedTracks, newTrackIndex) : new List<VideoEvent>();
            }

        }

        public static List<VideoEvent> GenerateProTypeTitlerEvents(Timecode start, Timecode length = null, string text = null, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1, TitlerChanger changer = null)
        {
            TextMediaProperties properties = new TextMediaProperties()
            {
                Text = text,
                MediaSeconds = length.ToMilliseconds() / 1000
            };
            return UltraPasteCommon.Vegas.Project.GenerateEvents<VideoEvent>(properties.GenerateProTypeTitlerMedia(presetName, changer), start, length, useMultipleSelectedTracks, newTrackIndex);
        }

        public static List<VideoEvent> GenerateLegacyTextEvents(Timecode start, Timecode length = null, string text = null, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1)
        {
            if (PlugInLegacyText == null)
            {
                return new List<VideoEvent>();
            }

            byte[] data = PlugInLegacyText.LoadDxtEffectPreset(presetName);
            int textStart = -1, textEnd = -1;

            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == 0x70) // "}"
                    {
                        textEnd = i;
                    }
                    else if (textStart < 0 && data[i] == 0x7B) // "{"
                    {
                        textStart = i;
                    }
                }
            }


            if (data == null || textStart < 4 || textEnd < textStart)
            {
                // default one
                data = new byte[] {0x00, 0x00, 0x00, 0x00, 0x0B, 0x00, 0x00, 0x00, 0xC8, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0, 0x3F, 0x02, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x8F, 0xC2, 0xF5, 0x3C, 0x8F, 0xC2, 0xF5, 0x3C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0xCD, 0xCC, 0x4C, 0x3E, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00,
                                   0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0xCD, 0xCC, 0x4C, 0x3D, 0xCD, 0xCC, 0xCC, 0x3D, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x17, 0x5D, 0x74, 0xD1, 0x45, 0x17, 0xED, 0x3F, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                   0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x85, 0x00, 0x00, 0x00, 0x7B, 0x5C, 0x72, 0x74, 0x66, 0x31, 0x5C, 0x61, 0x6E, 0x73, 0x69, 0x5C, 0x61, 0x6E, 0x73, 0x69, 0x63, 0x70, 0x67, 0x31, 0x32, 0x35, 0x32, 0x5C, 0x64, 0x65, 0x66, 0x66, 0x30, 0x5C, 0x64, 0x65, 0x66, 0x6C, 0x61, 0x6E, 0x67, 0x31, 0x30, 0x33, 0x33, 0x7B, 0x5C, 0x66, 0x6F, 0x6E, 0x74, 0x74, 0x62, 0x6C, 0x7B, 0x5C,
                                   0x66, 0x30, 0x5C, 0x66, 0x6E, 0x69, 0x6C, 0x5C, 0x66, 0x63, 0x68, 0x61, 0x72, 0x73, 0x65, 0x74, 0x30, 0x20, 0x41, 0x72, 0x69, 0x61, 0x6C, 0x3B, 0x7D, 0x7D, 0x0A, 0x5C, 0x76, 0x69, 0x65, 0x77, 0x6B, 0x69, 0x6E, 0x64, 0x34, 0x5C, 0x75, 0x63, 0x31, 0x5C, 0x70, 0x61, 0x72, 0x64, 0x5C, 0x71, 0x63, 0x5C, 0x62, 0x5C, 0x66, 0x73, 0x31, 0x34, 0x34, 0x20, 0x53, 0x61, 0x6D, 0x70, 0x6C, 0x65,
                                   0x5C, 0x70, 0x61, 0x72, 0x0A, 0x54, 0x65, 0x78, 0x74, 0x5C, 0x70, 0x61, 0x72, 0x0A, 0x7D, 0x0A, 0x00};
                textStart = 0x1CC;
                textEnd = 0x24E;
            }

            List<byte> l = data.Take(textStart - 4).ToList();

            byte[] bytesText = data.Skip(textStart).Take(textEnd - textStart + 1).ToArray();
            string txt = Encoding.UTF8.GetString(bytesText);
            RichTextBox rtb = new RichTextBox
            {
                Rtf = txt,
                Text = text
            };
            List<byte> newBytesText = new List<byte>(Encoding.UTF8.GetBytes(rtb.Rtf));
            newBytesText.AddRange(newBytesText.Count % 2 == 0 ? new byte[] { 0x0D, 0x0A, 0x00 } : new byte[] { 0x0A, 0x00 });
            l.AddRange(BitConverter.GetBytes(newBytesText.Count));
            l.AddRange(newBytesText);

            int oldLength = BitConverter.ToInt32(data, textStart - 4) + textStart;
            if (oldLength < data.Length)
            {
                l.AddRange(data.Skip(oldLength));
            }

            data = l.ToArray();

            string tempPresetName = string.Format("{0}_Temp", presetName);
            PlugInLegacyText.SaveDxtEffectPreset(tempPresetName, data);
            Media media = Media.CreateInstance(UltraPasteCommon.Vegas.Project, PlugInLegacyText);
            media.Generator.Preset = tempPresetName;
            PlugInLegacyText.DeleteDxtEffectPreset(tempPresetName);

            if (length.Nanos > 0)
            {
                media.Length = length;
            }

            return UltraPasteCommon.Vegas.Project.GenerateEvents<VideoEvent>(media, start, length, useMultipleSelectedTracks, newTrackIndex);
        }

        public static List<VideoEvent> GenerateSolidColorEvents(Timecode start, Timecode length = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1)
        {
            if (PlugInSolidColor == null)
            {
                return new List<VideoEvent>();
            }
            Media media = Media.CreateInstance(UltraPasteCommon.Vegas.Project, PlugInSolidColor);

            if (length.Nanos > 0)
            {
                media.Length = length;
            }

            OFXRGBAParameter c;
            if ((c = media.Generator.OFXEffect["Color"] as OFXRGBAParameter) != null)
            {
                OFXColor color = c.Value;
                color.A = 0;
                c.Value = color;
            }

            return UltraPasteCommon.Vegas.Project.GenerateEvents<VideoEvent>(media, start, length, useMultipleSelectedTracks, newTrackIndex);
        }

        unsafe public static List<VideoEvent> GenerateOfxEvents(PlugInNode plug, Timecode start, Timecode length = null, string text = null, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1)
        {
            if (plug == null)
            {
                return new List<VideoEvent>();
            }
            if (plug.IsGenerator())
            {
                Media media = Media.CreateInstance(UltraPasteCommon.Vegas.Project, plug);
                if (!string.IsNullOrEmpty(presetName))
                {
                    media.Generator.Preset = presetName;
                }
                if (length.Nanos > 0)
                {
                    media.Length = length;
                }
                media.Generator.SetTextStringParameters(text);

                if (plug.UniqueID == PlugInTextOfx?.UniqueID)
                {
                    (media.Generator.OFXEffect["font"] as OFXStringParameter).Value = (media.Generator.OFXEffect["name"] as OFXChoiceParameter).Value.Name;
                }
                return UltraPasteCommon.Vegas.Project.GenerateEvents<VideoEvent>(media, start, length, useMultipleSelectedTracks, newTrackIndex);
            }
            else
            {
                List<VideoEvent> vEvents = GenerateSolidColorEvents(start, length, useMultipleSelectedTracks, newTrackIndex);

                foreach (VideoEvent vEvent in vEvents)
                {
                    Effect ef = new Effect(plug);
                    vEvent.Effects.Add(ef);
                    ef.ApplyBeforePanCrop = true;
                    if (!string.IsNullOrEmpty(presetName))
                    {
                        ef.Preset = presetName;
                    }
                    ef.SetTextStringParameters(text);
                }
                return vEvents;
            }
        }



        public static List<OFXStringParameter> GetTextStringParameters(this Effect ef)
        {
            List<OFXStringParameter> ofxStrings = new List<OFXStringParameter>();
            if (ef == null || !ef.PlugIn.IsOFX)
            {
                return ofxStrings;
            }
            OFXStringParameter paraText;
            if ((paraText = (ef.OFXEffect["Text"] ?? ef.OFXEffect["text"] ?? ef.OFXEffect["Texts"] ?? ef.OFXEffect["texts"]) as OFXStringParameter) != null)
            {
                ofxStrings.Add(paraText);
            }
            else
            {
                string[] paraNames = ef.PlugIn.UniqueID == PlugInUniverseTextTypographic?.UniqueID ? new string[] { "68", "116" } : ef.PlugIn.UniqueID == PlugInUniverseTextHacker?.UniqueID ? new string[] { "0", "1" } : ef.PlugIn.UniqueID == PlugInOfxClock?.UniqueID ? new string[] { "Dig Clock Free Format" } : new string[0];
                foreach (string paraName in paraNames)
                {
                    OFXStringParameter p;
                    if ((p = ef.OFXEffect[paraName] as OFXStringParameter) != null)
                    {
                        ofxStrings.Add(p);
                    }
                }
            }
            return ofxStrings;
        }

        public static void SetTextStringParameters(this Effect ef, string text)
        {
            if (text == null)
            {
                return;
            }

            List<OFXStringParameter> ofxStrings = ef.GetTextStringParameters();

            if (ofxStrings.Count == 0)
            {
                return;
            }

            if (ef.PlugIn.UniqueID == PlugInTitlesAndText.UniqueID)
            {
                foreach (OFXStringParameter ofxString in ofxStrings)
                {
                    ofxString.Value = new RichTextBox { Rtf = ofxString.Value, Text = text }.Rtf;
                }
            }
            else
            {
                string[] strs = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);

                int count = Math.Min(ofxStrings.Count, strs.Length);

                for (int i = 0; i < count; i++)
                {
                    if (i == count - 1 && strs.Length > ofxStrings.Count)
                    {
                        ofxStrings[i].Value = string.Join("\n", strs);
                    }
                    else
                    {
                        ofxStrings[i].Value = strs[i];
                        strs[i] = string.Empty;
                    }
                }
            }
        }

        public static void SetTextPreset(this Effect ef, string preset)
        {
            if (preset == null)
            {
                return;
            }

            List<OFXStringParameter> ofxStrings = ef.GetTextStringParameters();

            if (ofxStrings.Count == 0)
            {
                return;
            }

            string[] strs = ofxStrings.Select(s => s.Value).ToArray();

            ef.Preset = preset;

            for (int i = 0; i < ofxStrings.Count; i++)
            {
                ofxStrings[i].Value = ef.PlugIn.UniqueID == PlugInTitlesAndText?.UniqueID ? new RichTextBox { Rtf = ofxStrings[i].Value, Text = new RichTextBox { Rtf = strs[i] }.Text }.Rtf : strs[i];
            }
        }

        public static string[] GetAvailablePresets(this PlugInNode plug)
        {
            if (plug == null)
            {
                return new string[0];
            }
            else if (plug.IsOFX)
            {
                List<string> l = new List<string>();
                foreach (EffectPreset p in plug.Presets)
                {
                    l.Add(p.Name);
                }
                return l.ToArray();
            }
            else
            {
                return plug.GetAvailableDxtPresets();
            }
        }
    }
}