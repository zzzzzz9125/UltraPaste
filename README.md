# UltraPaste! - Next-Level File Import for VEGAS Pro

## Overview
**UltraPaste!** introduces a revolutionary file import method for VEGAS Pro. With a single shortcut, you can import nearly any file type: regular media files, clipboard images, SRT subtitles, and even REAPER clipboard data! Simply `Ctrl+C` and `Ctrl+Shift+V` to instantly import files into the VEGAS timeline.
![UltraPaste!](https://github.com/user-attachments/assets/23d6db4a-f341-463b-b64e-f2c588bbd7dd)

---

## Installation
**Current Version:** v1.00  
**Supported Versions:** Sony VEGAS Pro 13 - Magix VEGAS Pro 22  

### How to Install
1. Place all `.dll` files in the extensions folder like:  
   - `C:\ProgramData\Vegas Pro\Application Extensions\`  
   - or `C:\ProgramData\Sony\Vegas Pro\Application Extensions\` for Sony

2. After installation, access the dockable window via **Tools → UltraPaste!**.

![image](https://github.com/user-attachments/assets/bbfa688e-4b82-4ff7-a5ee-3ebcf043251a)

---

## Features

### Core Functionality: UltraPaste!  
Bind a keyboard shortcut (**Recommended:** `Ctrl+Shift+V`) under **Options → Customize Keyboard**.  
![image](https://github.com/user-attachments/assets/5599b05b-61f5-44b2-be87-4508c2f01320)


#### Import Logic
- **Basic File Import:** Select any media files → `Ctrl+C` → Run UltraPaste! shortcut to import into the selected track.  
- **Clipboard Compatibility:**  
  - Clipboard images  
  - Plain text  
  - REAPER clipboard data  

---

### UltraPaste! Window (Settings)

#### General
![image](https://github.com/user-attachments/assets/36f9eb2b-06ec-4f08-bfe6-3eb71d261796)

- **Excluded Files:** Block specific files using DOS expressions (e.g., `*.sfvp0`, `*.sfap0` for VEGAS proxy files).  

#### Clipboard Image
![image](https://github.com/user-attachments/assets/3c604869-eae5-4152-8f18-71b5503f8a86)

- **Start Position:**  
  - Cursor  
  - Playback Cursor  
  - Project Start  
- **Move Cursor to End:** Auto-shift cursor to the end of new Events.  
- **File Path:**  
  - Default: `%PROJECTFOLDER%\Clipboard\<yyyyMMdd_HHmmss>.png`
  - You should separate the timecode with "<>". Timecode formatting rules: [Microsoft Docs](https://learn.microsoft.com/zh-cn/dotnet/api/system.datetime.tostring).  

#### REAPER Data
![image](https://github.com/user-attachments/assets/aba0e536-c9ec-465a-bd00-35dbf1cef250)

**Key Feature:** Full REAPER ↔ VEGAS clipboard interoperability!  
- REAPER Item Properties → VEGAS Event Properties  
- REAPER Item/Track Envelopes → VEGAS Track Envelopes  
- REAPER Stretch Markers → VEGAS Audio Stretch/Video Speed Envelopes  
- REAPER Project Files → Multi-Track Import

![image](https://github.com/user-attachments/assets/f77a8510-6720-4593-8107-45de00f59034)
![image](https://github.com/user-attachments/assets/a5f68512-8669-4063-9b51-c73dbe866e0a)

**Note:** Audio pitch parameters are unsupported in VEGAS versions <15.

 
- **Close Start Gap:** Adjust import start position to the first Item's start.  
- **Add Video Streams:** Attach video streams to imported REAPER Items.  

#### PSD Images
![image](https://github.com/user-attachments/assets/c2d4b2fb-2d11-4811-814c-7526ed159257)

- **Expand All Layers:** Auto-expand multi-layer PSDs into separate tracks.  
- **Add Other Layers:** Append remaining layers above/below selected PSD layers.  

#### Subtitles
![image](https://github.com/user-attachments/assets/5e041e53-515e-4133-b623-ff1941626647)

**Supported Formats:** Plain text, TXT, SRT, LRC.  
- **Generator Types:**  
  - Native: `Titles & Text`, `ProType Titler`, `(Legacy) Text` 
  - Plugins: `Ignite Text`, `Ignite 360 Text`, `uni. Typographic`, `uni. Hacker Text`, `TextOFX`, `OFX Clock`  
- **Preset Name:** Use saved FX presets (DXT plugins require user-saved presets).  
- **Max Characters:** Auto-wrap lines (0 = no limit).  
- **Ignore Word:** Disable for word-aware wrapping (useful for non-spaced languages like Chinese).  
- **Multi-Tracks:** Split multi-line subtitles into overlapping tracks (0 = no limit).  
- **Default Length:** Default length for non-timed text, in seconds.  

**Pro Tip:** Highlight a timeline region before pasting!  

![image](https://github.com/user-attachments/assets/0e65cd30-e33b-4685-b07b-2958d095b261)

#### Media
![image](https://github.com/user-attachments/assets/4d555933-29c3-4ade-8d9d-83db4181d98d)

- **Add Method:** Across Time, Across Tracks, As Takes.  
- **Stream Type:** All/Video Only/Audio Only.  
- **Auto-Import Image Sequences:** Detects image sequences (e.g., `000000.png` to `114514.png`).  

#### Custom Rules
![image](https://github.com/user-attachments/assets/5f5ccb9e-0a78-471c-80a4-56ea362c27b0)

Set import rules for specific filenames (e.g., UVR5 stems: `1_*(*).wav;1_*(*).flac` → import as audio across tracks).  

#### VEGAS Data
![image](https://github.com/user-attachments/assets/31ba5f02-bded-4415-8605-852e008f60d4)

- **VEG Import:** Open Project File / Nest Project / Import Media.  
- **Paste Event Attributes:** Use selective paste (VEGAS Pro 14 and below fallback to basic paste).  
- **Run Script:** Execute `.cs`, `.js`, `.vb`, or `.dll` files.  
- **Generate Mixed Vegas Clipboard Data:** Experimental support for Sony/Magix version compatibility.  

---

## Text Input Window  
![image](https://github.com/user-attachments/assets/0416f3cc-bdcd-48fd-af6e-0234275c6b13)

A pre-input panel for subtitle generation:  
- **Preview Box:** Shows the next subtitle block with auto-split preview.  
- **User Input:** Type/paste text here.  
- **Apply Text Splitting:** Manually trigger text segmentation.  
- **Add to Timeline:** Generate subtitles and advance to the next block.
- **Universal Key:** Also use UltraPaste! key to add events. In addition, you can bind a keyboard shortcut to `Text Input`.
  
---

**Tip:** Always check the GitHub repo for updates and community support! 🚀  
