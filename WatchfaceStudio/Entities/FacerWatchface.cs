﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WatchfaceStudio.Editor;

namespace WatchfaceStudio.Entities
{
    public class FacerWatchface
    {
        public bool Changed;
        public Forms.WatchfaceEditorForm EditorForm;
        public FacerLayer SelectedLayer;

        // Project data
        public readonly FacerWatchfaceDescription Description;
        public Image PreviewImage;
        public readonly Dictionary<string, Image> Images;
        public readonly Dictionary<string, FacerCustomFont> CustomFonts;
        public readonly List<FacerLayer> Layers;
        public readonly Dictionary<int, WatchfaceRendererError> Errors = new Dictionary<int, WatchfaceRendererError>();

        public string AddImageFile(string imageFile, bool newImage = false)
        {
            var fileName = Path.GetFileName(imageFile) ?? string.Empty;
            var key = newImage ? string.Concat(Path.GetFileNameWithoutExtension(fileName), ".png") : fileName;
            var i = 0;
            while (Images.ContainsKey(key))
                key = string.Concat(fileName, "(", i++, ").png");
            try
            {
                var fs = File.OpenRead(imageFile);
                var img = Image.FromStream(fs);
                fs.Close();
                Images.Add(key, img);
            }
            catch {
                return null;
            }
            return key;
        }

        public string AddFontFile(string fontFile)
        {
            var key = Path.GetFileName(fontFile) ?? string.Empty;
            var i = 0;
            while (CustomFonts.ContainsKey(key))
                key = string.Concat(Path.GetFileName(fontFile), "(", i++, ")");
            CustomFonts.Add(key, new FacerCustomFont(fontFile));
            return key;
        }

        public FacerWatchface(string folder)
        {
            if (folder != null)
            {
                var lookForWatchface = Directory.EnumerateFiles(folder, "watchface.json", SearchOption.AllDirectories).ToList();
                if (!lookForWatchface.Any())
                    throw new Exception("watchface.json was not found");

                var watchfaceFile = lookForWatchface.First();
                var foundFolder = Path.GetDirectoryName(watchfaceFile);
                if (foundFolder != null && foundFolder != folder)
                {
                    folder = foundFolder;
                }

                var jsonString = File.ReadAllText(watchfaceFile);
                Layers = JsonConvert.DeserializeObject<List<FacerLayer>>(jsonString, new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Ignore });

                var descriptionFile = Path.Combine(folder, "description.json");
                if (!File.Exists(descriptionFile))
                    throw new Exception("description.json was not found");
                jsonString = File.ReadAllText(descriptionFile);
                Description = JsonConvert.DeserializeObject<FacerWatchfaceDescription>(jsonString, new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Ignore });

                var fs = File.OpenRead(Path.Combine(folder, "preview.png"));
                PreviewImage = Image.FromStream(fs);
                fs.Close();

                Images = new Dictionary<string, Image>();
                var imagesFolder = Path.Combine(folder, "images");
                var imageExtensions = new[] {".jpg", ".jpeg", ".bmp", ".gif", ".png", string.Empty};
                if (Directory.Exists(imagesFolder))
                    foreach (var imageFile in 
                        Directory.EnumerateFiles(imagesFolder)
                        .Where(x => imageExtensions.Any(f => f==Path.GetExtension(x))))
                    {
                        AddImageFile(imageFile, false);
                    }
                CustomFonts = new Dictionary<string, FacerCustomFont>();
                var fontsFolder = Path.Combine(folder, "fonts");
                if (Directory.Exists(fontsFolder))
                    foreach (var fontFile in Directory.EnumerateFiles(fontsFolder, "*.ttf"))
                    {
                        AddFontFile(fontFile);
                    }
            }
            else
            {
                Layers = new List<FacerLayer>();
                Description = new FacerWatchfaceDescription { 
                    title = "Untitled",
                    id = Guid.NewGuid().ToString("N").ToLower() };
                Images = new Dictionary<string, Image>();
                CustomFonts = new Dictionary<string, FacerCustomFont>();
            }
        }

        internal bool SaveTo(string folderPath)
        {
            //fix layers
            for (var i = 0; i < Layers.Count; i++)
            {
                Layers[i].id = i;
                if (Layers[i].type == "text")
                {
                    if (string.IsNullOrEmpty(Layers[i].low_power_color))
                        Layers[i].low_power_color = "0";
                    if (string.IsNullOrEmpty(Layers[i].bgcolor))
                        Layers[i].bgcolor = "0";
                }
            }

            var preview = FacerWatcfaceRenderer.Render(this, EditorContext.WatchType, EWatchfaceOverlay.None, false, null, false);
            preview.Save(Path.Combine(folderPath, "preview.png"), System.Drawing.Imaging.ImageFormat.Png);

            var watchfileContent = JsonConvert.SerializeObject(Layers, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, StringEscapeHandling = StringEscapeHandling.EscapeNonAscii });
            File.WriteAllText(Path.Combine(folderPath, "watchface.json"), watchfileContent);

            var descriptionContent = JsonConvert.SerializeObject(Description, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, StringEscapeHandling = StringEscapeHandling.EscapeNonAscii });
            File.WriteAllText(Path.Combine(folderPath, "description.json"), descriptionContent);

            if (CustomFonts.Count > 0)
            {
                Directory.CreateDirectory(Path.Combine(folderPath, "fonts"));
                foreach (var kvp in CustomFonts)
                {
                    File.WriteAllBytes(Path.Combine(folderPath, "fonts", kvp.Key), kvp.Value.FileBytes);
                }
            }

            if (Images.Count > 0)
            {
                Directory.CreateDirectory(Path.Combine(folderPath, "images"));
                foreach (var kvp in Images)
                {
                    kvp.Value.Save(Path.Combine(folderPath, "images", kvp.Key), System.Drawing.Imaging.ImageFormat.Png);
                }
            }

            return true;
        }
    }
}
