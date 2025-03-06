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

using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste
{
    public class TextMediaGenerator
    {
        public const string UID_PROTYPE_TITLER = "{53FC0B44-BD58-4716-A90F-3EB43168DE81}";
        public const string UID_TITLES_AND_TEXT = "{Svfx:com.vegascreativesoftware:titlesandtext}";
        public const string UID_TITLES_AND_TEXT_SONY = "{Svfx:com.sonycreativesoftware:titlesandtext}";
        public const string UID_TEXT_OFX = "{Svfx:no.openfx.Text}";
        public static PlugInNode PlugInProTypeTitler
        {
            get
            {
                return UltraPasteCommon.myVegas.Generators.FindChildByUniqueID(UID_PROTYPE_TITLER);
            }
        }
        public static PlugInNode PlugInTitlesAndText
        {
            get
            {
                return UltraPasteCommon.myVegas.Generators.FindChildByUniqueID(UID_TITLES_AND_TEXT) ?? UltraPasteCommon.myVegas.Generators.FindChildByUniqueID(UID_TITLES_AND_TEXT_SONY);
            }
        }
        public static PlugInNode PlugInTextOfx
        {
            get
            {
                return UltraPasteCommon.myVegas.Generators.FindChildByUniqueID(UID_TEXT_OFX);
            }
        }

        public delegate void TitlerChanger(Titler titler);

        public class TextMediaProperties
        {
            public Size MediaSize = new Size(UltraPasteCommon.myVegas.Project.Video.Width, UltraPasteCommon.myVegas.Project.Video.Height);
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

                string presetXml = PlugInProTypeTitler.LoadDxtEffectPreset(preset);

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
                string tmpPresetName = "Temp";
                PlugInProTypeTitler.SaveDxtEffectPreset(tmpPresetName, titler.SerializeXml());
                Media myMedia = Media.CreateInstance(UltraPasteCommon.myVegas.Project, PlugInProTypeTitler);
                myMedia.Generator.Preset = tmpPresetName;
                myMedia.GetVideoStreamByIndex(0).Size = MediaSize;
                if (MediaMilliseconds > 0)
                {
                    myMedia.Length = Timecode.FromMilliseconds(MediaMilliseconds);
                }
                return myMedia;
            }
        }

        public static List<VideoEvent> GenerateProTypeTitlerEvents(Timecode start, Timecode length = null, string text = null, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1, TitlerChanger changer = null)
        {
            TextMediaProperties properties = new TextMediaProperties()
            {
                Text = text,
                MediaMilliseconds = length.ToMilliseconds()
            };
            return UltraPasteCommon.myVegas.Project.GenerateEvents<VideoEvent>(properties.GenerateProTypeTitlerMedia(presetName, changer), start, length, useMultipleSelectedTracks, newTrackIndex);
        }

        public static List<VideoEvent> GenerateTitlesAndTextEvents(Timecode start, Timecode length = null, string text = null, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1)
        {
            Media media = Media.CreateInstance(UltraPasteCommon.myVegas.Project, PlugInTitlesAndText, presetName);
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

            return UltraPasteCommon.myVegas.Project.GenerateEvents<VideoEvent>(media, start, length, useMultipleSelectedTracks, newTrackIndex);
        }

        // support for "Text OFX" (see: https://text.openfx.no/)
        public static List<VideoEvent> GenerateTextOfxEvents(Timecode start, Timecode length = null, string text = null, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1)
        {
            if (PlugInTextOfx == null)
            {
                return new List<VideoEvent>();
            }
            Media media = Media.CreateInstance(UltraPasteCommon.myVegas.Project, PlugInTextOfx, presetName);
            if (length.Nanos > 0)
            {
                media.Length = length;
            }

            (media.Generator.OFXEffect["text"] as OFXStringParameter).Value = text;
            (media.Generator.OFXEffect["font"] as OFXStringParameter).Value = (media.Generator.OFXEffect["name"] as OFXChoiceParameter).Value.Name;

            return UltraPasteCommon.myVegas.Project.GenerateEvents<VideoEvent>(media, start, length, useMultipleSelectedTracks, newTrackIndex);
        }
    }
}