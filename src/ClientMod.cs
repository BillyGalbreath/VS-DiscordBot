using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace DiscordBot;

// ReSharper disable All
public class ClientMod {
    private static readonly Dictionary<SkinnablePart, SkinnablePartVariant> Parts = new();

    private readonly ICoreClientAPI _capi;
    private readonly ClientPlatformAbstract _platform;

    private GuiDialogCreateCharacter? _dialog;
    private bool _findNextSkin;
    private string? _takeScreenshot;

    private SKBitmap? _bitmap, _rotated, _cropped;

    public ClientMod(ICoreClientAPI api) {
        _capi = api;

        _platform = (ClientPlatformAbstract)typeof(ClientMain)
            .GetField("Platform", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue((ClientMain)_capi.World)!;

        ClientEventManager eventManager = (ClientEventManager)typeof(ClientMain)
            .GetField("eventManager", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue((ClientMain)_capi.World)!;

        eventManager.RegisterRenderer(_ => {
            if (_takeScreenshot != null) {
                Screenshot(_takeScreenshot);
            }
        }, EnumRenderStage.Done, "uh...", 2.0);
    }

    public void Start() {
        _dialog = new GuiDialogCreateCharacter(_capi, _capi.ModLoader.GetModSystem<CharacterSystem>());
        _dialog.PrepAndOpen();
        if (!_dialog.IsOpened()) {
            _dialog.TryOpen();
        }

        ThreadPool.QueueUserWorkItem(_ => {
            EntityBehaviorExtraSkinnable ebes = _capi.World.Player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
            BaseSkin(ebes);
            EyeColor(ebes);
            HairBase(ebes);
            HairExtra(ebes);
            Expression(ebes);
            Mustache(ebes);
            Beard(ebes);
            _capi.Event.EnqueueMainThreadTask(() => { _dialog?.TryClose(); }, "try close");
        });
    }

    private void BaseSkin(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart baseskin = ebes.AvailableSkinPartsByCode.Get("baseskin")!;
        foreach (SkinnablePartVariant? variant in baseskin.Variants) {
            Parts.Clear();
            Parts.Add(baseskin, variant);
            //EyeColor(ebes);
            End(ebes, baseskin);
        }
    }

    private void EyeColor(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart eyecolor = ebes.AvailableSkinPartsByCode.Get("eyecolor")!;
        foreach (SkinnablePartVariant? variant in eyecolor.Variants) {
            Parts.Clear();
            Parts.Add(eyecolor, variant);
            //HairBase(ebes);
            End(ebes, eyecolor);
        }
    }

    private void HairBase(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart hairbase = ebes.AvailableSkinPartsByCode.Get("hairbase")!;
        foreach (SkinnablePartVariant? variant in hairbase.Variants) {
            Parts.Clear();
            Parts.Add(hairbase, variant);
            //HairExtra(ebes);
            HairColor(ebes, hairbase);
        }
    }

    private void HairExtra(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart hairbase = ebes.AvailableSkinPartsByCode.Get("hairbase")!;
        SkinnablePart hairextra = ebes.AvailableSkinPartsByCode.Get("hairextra")!;
        foreach (SkinnablePartVariant? variant in hairextra.Variants) {
            Parts.Clear();
            Parts.Add(hairbase, hairbase.Variants[0]);
            Parts.Add(hairextra, variant);
            //Expression(ebes);
            HairColor(ebes, hairextra);
        }
    }

    private void Expression(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart hairextra = ebes.AvailableSkinPartsByCode.Get("hairextra")!;
        SkinnablePart expression = ebes.AvailableSkinPartsByCode.Get("facialexpression")!;
        foreach (SkinnablePartVariant? variant in expression.Variants) {
            Parts.Clear();
            Parts.Add(hairextra, hairextra.Variants[0]);
            Parts.Add(expression, variant);
            //Mustache(ebes);
            End(ebes, expression);
        }
    }

    private void Mustache(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart mustache = ebes.AvailableSkinPartsByCode.Get("mustache")!;
        foreach (SkinnablePartVariant? variant in mustache.Variants) {
            Parts.Clear();
            Parts.Add(mustache, variant);
            //Beard(ebes);
            HairColor(ebes, mustache);
        }
    }

    private void Beard(EntityBehaviorExtraSkinnable ebes) {
        SkinnablePart mustache = ebes.AvailableSkinPartsByCode.Get("mustache")!;
        SkinnablePart beard = ebes.AvailableSkinPartsByCode.Get("beard")!;
        foreach (SkinnablePartVariant? variant in beard.Variants) {
            Parts.Clear();
            Parts.Add(mustache, mustache.Variants[0]);
            Parts.Add(beard, variant);
            HairColor(ebes, beard);
        }
    }

    private void HairColor(EntityBehaviorExtraSkinnable ebes, SkinnablePart type) {
        SkinnablePart haircolor = ebes.AvailableSkinPartsByCode.Get("haircolor")!;
        foreach (SkinnablePartVariant? variant in haircolor.Variants) {
            Parts.Remove(haircolor);
            Parts.Add(haircolor, variant);

            End(ebes, type);
        }
    }

    private void End(EntityBehaviorExtraSkinnable ebes, SkinnablePart type) {
        _capi.Event.EnqueueMainThreadTask(() => { FinalResult(ebes, type); }, "final result");

        _findNextSkin = false;
        while (!_findNextSkin) {
            Thread.Sleep(1);
        }
    }

    private void PickPart(EntityBehaviorExtraSkinnable ebes, SkinnablePart skinpart, SkinnablePartVariant variant) {
        ebes.selectSkinPart(skinpart.Code, variant.Code);
        int index = Array.IndexOf(skinpart.Variants, variant);
        if (skinpart is { Type: EnumSkinnableType.Texture, UseDropDown: false }) {
            _dialog?.Composers["createcharacter"].ColorListPickerSetValue("picker-" + skinpart.Code, index);
        } else {
            _dialog?.Composers["createcharacter"].GetDropDown("dropdown-" + skinpart.Code).SetSelectedIndex(index);
        }
    }

    private void FinalResult(EntityBehaviorExtraSkinnable ebes, SkinnablePart type) {
        EntitySkinnableShapeRenderer essr = (_capi.World.Player.Entity.Properties.Client.Renderer as EntitySkinnableShapeRenderer)!;
        essr.doReloadShapeAndSkin = false;
        foreach (KeyValuePair<SkinnablePart, SkinnablePartVariant> entry in Parts) {
            PickPart(ebes, entry.Key, entry.Value);
        }

        essr.doReloadShapeAndSkin = true;
        essr.TesselateShape();

        _capi.Event.RegisterCallback(_ => _takeScreenshot = type.Code, 1);
    }

    private void Screenshot(string type) {
        _takeScreenshot = null;

        _platform.LoadFrameBuffer(EnumFrameBuffer.Default);

        _bitmap = new SKBitmap(_platform.WindowSize.Width, _platform.WindowSize.Height, true);
        GL.ReadPixels(0, 0, _platform.WindowSize.Width, _platform.WindowSize.Height, PixelFormat.Bgra, PixelType.UnsignedByte, _bitmap.GetPixels());

        _rotated = new SKBitmap(_bitmap.Width, _bitmap.Height, _bitmap.ColorType, SKAlphaType.Opaque);
        using (SKCanvas surface = new(_rotated)) {
            surface.Translate(_bitmap.Width, _bitmap.Height);
            surface.RotateDegrees(180);
            surface.Scale(-1, 1, _bitmap.Width / 2F, 0);
            surface.DrawBitmap(_bitmap, 0, 0);
        }

        _cropped = new SKBitmap(256, 256);
        _rotated.ExtractSubset(_cropped, new SKRectI(835, 301, 1091, 557));

        //scaled = new SKBitmap(64, 64);
        //cropped.ScalePixels(scaled, SKFilterQuality.High);

        string path = Path.Combine(GamePaths.Screenshots, type);
        SkinnablePartVariant[] arr = Parts.Values.ToArray();
        for (int i = 0; i < arr.Length - 1; i++) {
            path = Path.Combine(path, arr[i].Code);
        }

        //string finalPath = PARTS.Values.Aggregate(Path.Combine(GamePaths.Screenshots, "hairbase"), (current, variant) => Path.Combine(current, Path.Combine(variant.Code))) + ".png";
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }

        using (Stream stream = File.OpenWrite(Path.Combine(path, $"{arr[^1].Code}.png"))) {
            _cropped.Encode(SKEncodedImageFormat.Png, 0).SaveTo(stream);
        }

        _findNextSkin = true;
    }
}
