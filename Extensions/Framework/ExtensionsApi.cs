﻿// This file is a part of MPDN Extensions.
// https://github.com/zachsaw/MPDN_Extensions
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using DirectShowLib;
using Mpdn.AudioScript;
using Mpdn.Config;
using Mpdn.RenderScript;

namespace Mpdn.Extensions.Framework
{
    public static class Gui
    {
        public static Control VideoBox
        {
            get { return PlayerControl.VideoPanel; }
        }

        public static Icon Icon
        {
            get { return PlayerControl.About.Icon; }
        }

        public static int FullScreenSeekBarHeight
        {
            get { return PlayerControl.FullScreenSeekBarHeight; }
        }
    }

    public static class ComThread
    {
        public static void Do(Action action)
        {
            PlayerControl.ComInvoke(action);
        }

        public static void DoAsync(Action action)
        {
            PlayerControl.ComInvokeAsync(action);
        }
    }

    public static class Extension
    {
        public static int InterfaceVersion
        {
            get { return 9; }
        }
    
        public static IList<IExtensionUi> Scripts
        {
            get { return RenderScripts.Concat<IExtensionUi>(AudioScripts).ToList(); }
        }

        public static IReadOnlyList<Assembly> Assemblies
        {
            get { return PlayerControl.ExtensionAssemblies; }
        }

        public static IReadOnlyList<IRenderScriptUi> RenderScripts
        {
            get { return PlayerControl.RenderScripts; }
        }

        public static IReadOnlyList<IAudioScriptUi> AudioScripts
        {
            get { return PlayerControl.AudioScripts; }
        }

        public static IReadOnlyList<IPlayerExtension> PlayerExtensions
        {
            get { return PlayerControl.PlayerExtensions; }
        }

        public static IAudioScript AudioScript
        {
            get { return PlayerControl.ActiveAudioScript; }
        }

        public static Guid AudioScriptGuid
        {
            get { return PlayerControl.ActiveAudioScriptGuid; }
        }

        public static IRenderScript RenderScript
        {
            get { return PlayerControl.ActiveRenderScript; }
        }

        public static Guid RenderScriptGuid
        {
            get { return PlayerControl.ActiveRenderScriptGuid; }
        }

        public static void SetAudioScript(Guid scriptGuid)
        {
            PlayerControl.ActiveAudioScriptGuid = scriptGuid;
        }

        public static void SetRenderScript(Guid scriptGuid)
        {
            PlayerControl.ActiveRenderScriptGuid = scriptGuid;
        }

        public static void Refresh<TScript>()
            where TScript : class, IScript
        {
            if (typeof(IRenderScript).IsAssignableFrom(typeof(TScript)))
                RefreshRenderScript();
            else if (typeof(IAudioScript).IsAssignableFrom(typeof(TScript)))
                RefreshAudioScript();
            else throw new ArgumentOutOfRangeException("Unrecognised Script type");
        }

        private static void RefreshAudioScript()
        {
            SetAudioScript(AudioScriptGuid);
        }

        public static void RefreshRenderScript()
        {
            SetRenderScript(RenderScriptGuid);
        }
    }

    public static class Player
    {
        private static readonly List<string> s_RegisteredMediaExtensions = new List<string>(new[]
        {
            ".avi", ".mpg", ".mpeg", ".mpe", ".m1v", ".m2v", ".mpv2", ".mp2v", ".pva", ".evo", ".m2p", ".ts", ".tp",
            ".trp", ".m2t", ".m2ts", ".mts", ".rec", ".vob", ".mkv", ".webm", ".mp4", ".m4v", ".mp4v", ".mpv4",
            ".hdmov", ".mov", ".3gp", ".3gpp", ".3g2", ".3gp2", ".flv", ".f4v", ".ogm", ".ogv", ".rm", ".ram",
            ".rpm", ".rmm", ".rt", ".rp", ".smi", ".smil", ".wmv", ".wmp", ".wm", ".asf", ".smk", ".bik", ".fli",
            ".flc", ".flic", ".dsm", ".dsv", ".dsa", ".dss", ".ivf", ".swf", ".divx", ".rmvb", ".amv", ".ac3",
            ".dts", ".aif", ".aifc", ".aiff", ".alac", ".amr", ".ape", ".apl", ".au", ".snd", ".cda", ".flac",
            ".m4a", ".m4b", ".aac", ".mid", ".midi", ".rmi", ".mka", ".mp3", ".mpa", ".mp2", ".m1a", ".m2a", ".mpc",
            ".ofr", ".ofs", ".ogg", ".oga", ".opus", ".ra", ".tta", ".wav", ".wma", ".wv", ".aob", ".mlp", ".asx",
            ".m3u", ".m3u8", ".wvx", ".wax", ".wmx", ".bdmv"
        });

        private static readonly object s_RegExtLock = new object();
        private static Action<Exception> s_CustomExceptionHandler;

        public static string[] RegisteredMediaExtensions
        {
            get
            {
                lock (s_RegExtLock)
                {
                    return s_RegisteredMediaExtensions.ToArray();
                }
            }
        }

        public static Form ActiveForm
        {
            get { return PlayerControl.Form; }
        }

        public static class Stats
        {
            public static IPlayerStats Details
            {
                get { return PlayerControl.PlayerStats; }
            }

            public static void Show(bool show)
            {
                PlayerControl.ShowStats(show);
            }

            public static void Reset()
            {
                PlayerControl.ResetStats();
            }
        }

        public static PlayerState State
        {
            get { return PlayerControl.PlayerState; }
        }

        public static int Volume
        {
            get { return PlayerControl.Volume; }
            set { PlayerControl.Volume = value; }
        }

        public static bool Mute
        {
            get { return PlayerControl.Mute; }
            set { PlayerControl.Mute = value; }
        }

        public static class Playback
        {
            public static event EventHandler RateChanged
            {
                add { PlayerControl.PlaybackRateChanged += value; }
                remove { PlayerControl.PlaybackRateChanged -= value; }
            }

            public static event EventHandler Completed
            {
                add { PlayerControl.PlaybackCompleted += value; }
                remove { PlayerControl.PlaybackCompleted -= value; }
            }

            public static double Rate
            {
                get { return PlayerControl.PlaybackRate; }
                set { PlayerControl.PlaybackRate = value; }
            }

            public static double BaseRate
            {
                get { return PlayerControl.PlaybackBaseRate; }
                set { PlayerControl.PlaybackBaseRate = value; }
            }
        }

        public static class Config
        {
            public static event EventHandler Changed
            {
                add { PlayerControl.SettingsChanged += value; }
                remove { PlayerControl.SettingsChanged -= value; }
            }

            public static event EventHandler Committing
            {
                add { PlayerControl.SettingsCommitting += value; }
                remove { PlayerControl.SettingsCommitting -= value; }
            }

            public static event EventHandler Committed
            {
                add { PlayerControl.SettingsCommitted += value; }
                remove { PlayerControl.SettingsCommitted -= value; }
            }

            public static string Path
            {
                get { return PlayerControl.ConfigRootPath; }
            }

            public static Configuration Settings
            {
                get { return PlayerControl.PlayerSettings; }
            }

            public static void Refresh()
            {
                PlayerControl.RefreshSettings();
            }

            public static bool Commit()
            {
                return PlayerControl.CommitSettings();
            }
        }

        public static class FullScreenMode
        {
            public static event EventHandler Entering
            {
                add { PlayerControl.EnteringFullScreenMode += value; }
                remove { PlayerControl.EnteringFullScreenMode -= value; }
            }

            public static event EventHandler Entered
            {
                add { PlayerControl.EnteredFullScreenMode += value; }
                remove { PlayerControl.EnteredFullScreenMode -= value; }
            }

            public static event EventHandler Exiting
            {
                add { PlayerControl.ExitingFullScreenMode += value; }
                remove { PlayerControl.ExitingFullScreenMode -= value; }
            }

            public static event EventHandler Exited
            {
                add { PlayerControl.ExitedFullScreenMode += value; }
                remove { PlayerControl.ExitedFullScreenMode -= value; }
            }

            public static bool Active
            {
                get { return PlayerControl.InFullScreenMode; }
                set
                {
                    if (value == PlayerControl.InFullScreenMode)
                        return;

                    if (value)
                    {
                        PlayerControl.GoFullScreen();
                    }
                    else
                    {
                        PlayerControl.GoWindowed();
                    }
                }
            }
        }

        public static event EventHandler Loaded
        {
            add { PlayerControl.PlayerLoaded += value; }
            remove { PlayerControl.PlayerLoaded -= value; }
        }

        public static event EventHandler Closed
        {
            add { PlayerControl.FormClosed += value; }
            remove { PlayerControl.FormClosed -= value; }
        }

        public static event EventHandler PaintOverlay
        {
            add { PlayerControl.PaintOverlay += value; }
            remove { PlayerControl.PaintOverlay -= value; }
        }

        public static event EventHandler<PlayerStateEventArgs> StateChanged
        {
            add { PlayerControl.PlayerStateChanged += value; }
            remove { PlayerControl.PlayerStateChanged -= value; }
        }

        public static event EventHandler VolumeChanged
        {
            add { PlayerControl.VolumeChanged += value; }
            remove { PlayerControl.VolumeChanged -= value; }
        }

        public static event EventHandler<PlayerControlEventArgs<KeyEventArgs>> KeyDown
        {
            add { PlayerControl.KeyDown += value; }
            remove { PlayerControl.KeyDown -= value; }
        }

        public static event EventHandler<PlayerControlEventArgs<MouseEventArgs>> MouseWheel
        {
            add { PlayerControl.MouseWheel += value; }
            remove { PlayerControl.MouseWheel -= value; }
        }

        public static event EventHandler<PlayerControlEventArgs<MouseEventArgs>> MouseMove
        {
            add { PlayerControl.MouseMove += value; }
            remove { PlayerControl.MouseMove -= value; }
        }

        public static event EventHandler<PlayerControlEventArgs<MouseEventArgs>> MouseClick
        {
            add { PlayerControl.MouseClick += value; }
            remove { PlayerControl.MouseClick -= value; }
        }

        public static event EventHandler<PlayerControlEventArgs<MouseEventArgs>> MouseDoubleClick
        {
            add { PlayerControl.MouseDoubleClick += value; }
            remove { PlayerControl.MouseDoubleClick -= value; }
        }

        public static event EventHandler<PlayerControlEventArgs<DragEventArgs>> DragDrop
        {
            add { PlayerControl.DragDrop += value; }
            remove { PlayerControl.DragDrop -= value; }
        }

        public static event EventHandler<PlayerControlEventArgs<DragEventArgs>> DragEnter
        {
            add { PlayerControl.DragEnter += value; }
            remove { PlayerControl.DragEnter -= value; }
        }

        public static event EventHandler<CommandLineFileOpenEventArgs> CommandLineFileOpen
        {
            add { PlayerControl.CommandLineFileOpen += value; }
            remove { PlayerControl.CommandLineFileOpen -= value; }
        }

        [Obsolete]
        public static class Window
        {
            public static void FitAspectRatio()
            {
                PlayerControl.FitAspectRatio();
            }

            public static void FitPercentage(int percent)
            {
                PlayerControl.FitPercentage(percent);
            }

            public static void ResetSize()
            {
                PlayerControl.ResetWindowSize();
            }
        }

        public static void HandleException(Exception exception)
        {
            var handler = s_CustomExceptionHandler;
            if (handler != null)
            {
                handler(exception);
            }
            else
            {
                GuiThread.DoAsync(() => PlayerControl.HandleException(exception));
            }
        }

        public static void RedirectExceptionHandler(Action<Exception> handler)
        {
            s_CustomExceptionHandler = handler;
        }

        public static void RemoveExceptionHandlerRedirection()
        {
            s_CustomExceptionHandler = null;
        }

        public static class OsdText
        {
            public static void Show(string text, int durationMs = 1000)
            {
                PlayerControl.ShowOsdText(text, durationMs);
            }

            public static void Hide()
            {
                PlayerControl.HideOsdText();
            }
        }

        public static IText CreateText(string font, int size, TextFontStyle style)
        {
            return PlayerControl.CreateText(font, size, style);
        }

        public static void RegisterMediaFileExtension(string extension, params string[] extensions)
        {
            lock (s_RegExtLock)
            {
                var exts = (new[] {extension}.Concat(extensions)).ToList();
                PlayerControl.RegisterMediaFileExtensions(exts);
                s_RegisteredMediaExtensions.AddRange(exts.Where(e => e.StartsWith(".")).Select(e => e.ToLower()));
            }
        }

        public static void UnregisterMediaFileExtension(string extension, params string[] extensions)
        {
            lock (s_RegExtLock)
            {
                var exts = (new[] {extension}.Concat(extensions)).ToList();
                PlayerControl.UnregisterMediaFileExtensions(exts);
                var removals = exts.Where(e => e.StartsWith(".")).Select(e => e.ToLower());
                s_RegisteredMediaExtensions.RemoveAll(e => removals.Contains(e));
            }
        }

        public static void ClearScreen()
        {
            PlayerControl.ClearScreen();
        }

        public static void ShowOptionsDialog()
        {
            PlayerControl.ShowOptionsDialog();
        }

        public static IReadOnlyList<DirectShow.Filter> Filters
        {
            get { return PlayerControl.Filters; }
        }
    }

    public static class Media
    {
        public static string FilePath
        {
            get { return PlayerControl.MediaFilePath; }
        }

        public static long Duration
        {
            get { return PlayerControl.MediaDuration; }
        }

        public static long Position
        {
            get { return PlayerControl.MediaPosition; }
        }

        public static IReadOnlyList<Chapter> Chapters
        {
            get { return PlayerControl.Chapters; }
        }

        public static event EventHandler AudioTrackChanged
        {
            add { PlayerControl.AudioTrackChanged += value; }
            remove { PlayerControl.AudioTrackChanged -= value; }
        }

        public static event EventHandler VideoTrackChanged
        {
            add { PlayerControl.VideoTrackChanged += value; }
            remove { PlayerControl.VideoTrackChanged -= value; }
        }

        public static event EventHandler SubtitleTrackChanged
        {
            add { PlayerControl.SubtitleTrackChanged += value; }
            remove { PlayerControl.SubtitleTrackChanged -= value; }
        }

        public static MediaTrack AudioTrack
        {
            get { return PlayerControl.ActiveAudioTrack; }
            set { SelectAudioTrack(value); }
        }

        public static MediaTrack VideoTrack
        {
            get { return PlayerControl.ActiveVideoTrack; }
            set { SelectVideoTrack(value); }
        }

        public static MediaTrack SubtitleTrack
        {
            get { return PlayerControl.ActiveSubtitleTrack; }
            set { SelectSubtitleTrack(value); }
        }

        public static void SelectAudioTrack(MediaTrack track, bool showOsd = true)
        {
            PlayerControl.SelectAudioTrack(track, showOsd);
        }

        public static void SelectVideoTrack(MediaTrack track, bool showOsd = true)
        {
            PlayerControl.SelectVideoTrack(track, showOsd);
        }

        public static void SelectSubtitleTrack(MediaTrack track, bool showOsd = true)
        {
            PlayerControl.SelectSubtitleTrack(track, showOsd);
        }

        public static IReadOnlyList<MediaTrack> AudioTracks
        {
            get { return PlayerControl.AudioTracks; }
        }

        public static IReadOnlyList<MediaTrack> VideoTracks
        {
            get { return PlayerControl.VideoTracks; }
        }

        public static IReadOnlyList<MediaTrack> SubtitleTracks
        {
            get { return PlayerControl.SubtitleTracks; }
        }

        public static VideoInfo VideoInfo
        {
            get { return PlayerControl.VideoInfo; }
        }

        public static AMMediaType VideoMediaType
        {
            get { return PlayerControl.VideoMediaType; }
        }

        public static event EventHandler Loaded
        {
            add { PlayerControl.MediaLoaded += value; }
            remove { PlayerControl.MediaLoaded -= value; }
        }

        public static event EventHandler<MediaLoadingEventArgs> Loading
        {
            add { PlayerControl.MediaLoading += value; }
            remove { PlayerControl.MediaLoading -= value; }
        }

        public static IMedia Load(string filename)
        {
            return PlayerControl.LoadMedia(filename);
        }

        public static void Open(IMedia media, bool play = true, bool showOsd = true)
        {
            PlayerControl.OpenMedia(media, play, showOsd);
        }

        public static void Open(string filename, bool play = true, bool showOsd = true)
        {
            PlayerControl.OpenMedia(filename, play, showOsd);
        }

        public static void Play(bool showOsd = true)
        {
            PlayerControl.PlayMedia(showOsd);
        }

        public static void Pause(bool showOsd = true)
        {
            PlayerControl.PauseMedia(showOsd);
        }

        public static void Stop()
        {
            PlayerControl.StopMedia();
        }

        public static void Close()
        {
            PlayerControl.CloseMedia();
        }

        public static void Seek(long usec)
        {
            PlayerControl.SeekMedia(usec);
        }

        public static void Step()
        {
            PlayerControl.StepMedia();
        }

        public static class Frame
        {
            public static event EventHandler<FrameEventArgs> Decoded
            {
                add { PlayerControl.FrameDecoded += value; }
                remove { PlayerControl.FrameDecoded -= value; }
            }

            public static event EventHandler<FrameEventArgs> Rendered
            {
                add { PlayerControl.FrameRendered += value; }
                remove { PlayerControl.FrameRendered -= value; }
            }

            public static event EventHandler<FrameEventArgs> Presented
            {
                add { PlayerControl.FramePresented += value; }
                remove { PlayerControl.FramePresented -= value; }
            }
        }
    }

    public static class GuiThread
    {
        public static void Do(Action action)
        {
            Gui.VideoBox.Invoke(action);
        }

        public static void DoAsync(Action action)
        {
            Gui.VideoBox.BeginInvoke(action);
        }
    }
}
