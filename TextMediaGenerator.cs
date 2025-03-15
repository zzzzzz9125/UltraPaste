#if !Sony
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

namespace UltraPaste
{
    public class TextMediaGenerator
    {
        public const string UID_LEGACY_TEXT = "{0FE8789D-0C47-442A-AFB0-0DAF97669317}";
        public const string UID_PROTYPE_TITLER = "{53FC0B44-BD58-4716-A90F-3EB43168DE81}";
        public const string UID_TITLES_AND_TEXT = "{Svfx:com.vegascreativesoftware:titlesandtext}";
        public const string UID_TITLES_AND_TEXT_SONY = "{Svfx:com.sonycreativesoftware:titlesandtext}";
        public const string UID_TEXT_OFX = "{Svfx:no.openfx.Text}";
        public static PlugInNode PlugInProTypeTitler
        {
            get
            {
                return UltraPasteCommon.Vegas.Generators.FindChildByUniqueID(UID_PROTYPE_TITLER);
            }
        }
        public static PlugInNode PlugInTitlesAndText
        {
            get
            {
                return UltraPasteCommon.Vegas.Generators.FindChildByUniqueID(UID_TITLES_AND_TEXT) ?? UltraPasteCommon.Vegas.Generators.FindChildByUniqueID(UID_TITLES_AND_TEXT_SONY);
            }
        }
        public static PlugInNode PlugInLegacyText
        {
            get
            {
                return UltraPasteCommon.Vegas.Generators.FindChildByUniqueID(UID_LEGACY_TEXT);
            }
        }
        public static PlugInNode PlugInTextOfx
        {
            get
            {
                return UltraPasteCommon.Vegas.Generators.FindChildByUniqueID(UID_TEXT_OFX);
            }
        }

        public delegate void TitlerChanger(Titler titler);

        public class TextMediaProperties
        {
            public Size MediaSize = new Size(UltraPasteCommon.Vegas.Project.Video.Width, UltraPasteCommon.Vegas.Project.Video.Height);
            public double MediaMilliseconds = 5000;
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
                if (MediaMilliseconds > 0)
                {
                    myMedia.Length = Timecode.FromMilliseconds(MediaMilliseconds);
                }
                return myMedia;
            }
        }

        public static List<VideoEvent> GenerateTextEvents(Timecode start, Timecode length = null, string text = null, int type = 0, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1)
        {
            List<VideoEvent> evs = type == 1 ? GenerateProTypeTitlerEvents(start, length, text, presetName, useMultipleSelectedTracks, newTrackIndex)
                                 : type == 2 ?    GenerateLegacyTextEvents(start, length, text, presetName, useMultipleSelectedTracks, newTrackIndex)
                                 : type == 3 ?       GenerateTextOfxEvents(start, length, text, presetName, useMultipleSelectedTracks, newTrackIndex)
                                             : GenerateTitlesAndTextEvents(start, length, text, presetName, useMultipleSelectedTracks, newTrackIndex);
            return evs;
        }

        public static List<VideoEvent> GenerateProTypeTitlerEvents(Timecode start, Timecode length = null, string text = null, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1, TitlerChanger changer = null)
        {
            TextMediaProperties properties = new TextMediaProperties()
            {
                Text = text,
                MediaMilliseconds = length.ToMilliseconds()
            };
            return UltraPasteCommon.Vegas.Project.GenerateEvents<VideoEvent>(properties.GenerateProTypeTitlerMedia(presetName, changer), start, length, useMultipleSelectedTracks, newTrackIndex);
        }

        public static List<VideoEvent> GenerateTitlesAndTextEvents(Timecode start, Timecode length = null, string text = null, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1)
        {
            Media media = Media.CreateInstance(UltraPasteCommon.Vegas.Project, PlugInTitlesAndText, presetName);
            if (length.Nanos > 0)
            {
                media.Length = length;
            }

            if (!string.IsNullOrEmpty(text))
            {
                OFXStringParameter textPara = (OFXStringParameter)media.Generator.OFXEffect["Text"];
                RichTextBox rtb = new RichTextBox
                {
                    Rtf = textPara.Value,
                    Text = text
                };
                textPara.Value = rtb.Rtf;
            }

            return UltraPasteCommon.Vegas.Project.GenerateEvents<VideoEvent>(media, start, length, useMultipleSelectedTracks, newTrackIndex);
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

        // support for "Text OFX" (see: https://text.openfx.no/)
        public static List<VideoEvent> GenerateTextOfxEvents(Timecode start, Timecode length = null, string text = null, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1)
        {
            if (PlugInTextOfx == null)
            {
                return new List<VideoEvent>();
            }
            Media media = Media.CreateInstance(UltraPasteCommon.Vegas.Project, PlugInTextOfx, presetName);
            if (length.Nanos > 0)
            {
                media.Length = length;
            }

            (media.Generator.OFXEffect["text"] as OFXStringParameter).Value = text;
            (media.Generator.OFXEffect["font"] as OFXStringParameter).Value = (media.Generator.OFXEffect["name"] as OFXChoiceParameter).Value.Name;

            return UltraPasteCommon.Vegas.Project.GenerateEvents<VideoEvent>(media, start, length, useMultipleSelectedTracks, newTrackIndex);
        }
    }
}