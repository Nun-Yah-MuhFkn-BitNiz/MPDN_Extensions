// This file is a part of MPDN Extensions.
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
// 
using System;
using System.Collections.Generic;
using System.Linq;
using Mpdn.PlayerExtensions;
using YAXLib;

namespace Mpdn.RenderScript
{
    namespace Mpdn.Presets
    {
        public class Selector<TOption> : RenderChain
            where TOption : RenderChain, new()
        {
            public List<TOption> Options { get; set; }

            public int SelectedIndex { get; set; }

            [YAXDontSerialize]
            public TOption SelectedOption { get { return Options.ElementAtOrDefault(SelectedIndex); } }

            public Selector()
            {
                Options = new List<TOption>();
                SelectedIndex = -1;
            }

            public override IFilter CreateFilter(IResizeableFilter sourceFilter)
            {
                if (SelectedOption != null)
                    return SelectedOption.CreateFilter(sourceFilter);
                else
                    return sourceFilter;
            }
        }

        public class RenderScriptPreset : RenderChain
        {
            [YAXAttributeForClass]
            public string Name { get; set; }

            [YAXAttributeForClass]
            public Guid Guid { get; set; }

            public IRenderChainUi Script { get; set; }

            public RenderScriptPreset()
            {
                Guid = Guid.NewGuid();
            }

            public override IFilter CreateFilter(IResizeableFilter sourceFilter)
            {
                if (Script != null)
                    return Script.GetChain().CreateFilter(sourceFilter);
                else
                    return sourceFilter;
            }
        }

        public class PresetSettings : Selector<RenderScriptPreset> 
        {
            private Guid HotkeyGuid = Guid.NewGuid();
            private string m_Hotkey;

            public string Name { get; set; }
            public string Hotkey 
            {
                get { return m_Hotkey; }
                set
                {
                    m_Hotkey = value;
                    RegisterHotkey();
                } 
            }

            public PresetSettings()
            {
                Name = "Preset Group";
                Hotkey = "";
            }

            private void RegisterHotkey()
            {
                DynamicHotkeys.RegisterHotkey(HotkeyGuid, Hotkey, IncrementSelection);
            }

            private void IncrementSelection()
            {
                if (Options.Count > 0)
                    SelectedIndex = (SelectedIndex + 1) % Options.Count;

                if (SelectedOption != null)
                    PlayerControl.ShowOsdText(Name + ": " + SelectedOption.Name);

                PlayerControl.SetRenderScript(PlayerControl.ActiveRenderScriptGuid);
            }
        }

        public class PresetRenderScript : RenderChainUi<PresetSettings, PresetDialog>
        {
            protected override string ConfigFileName
            {
                get { return "Mpdn.Presets"; }
            }

            public override ExtensionUiDescriptor Descriptor
            {
                get
                {
                    return new ExtensionUiDescriptor
                    {
                        Name = Settings.Name ?? "Preset Group",
                        Guid = new Guid("B1F3B882-3E8F-4A8C-B225-30C9ABD67DB1"),
                        Description = (Settings.SelectedOption == null) ? "Group of Presets" : Settings.SelectedOption.Name
                    };
                }
            }
        }
    }
}
