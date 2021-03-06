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
// 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace Mpdn.Extensions.PlayerExtensions.Subtitles
{
    public class OpenSubtitlesLanguageHandler
    {
        public static CultureInfo InvariantCulture { get; set; }
        public static List<CultureInfo> GetListCulture()
        {
            var listCulture = new List<CultureInfo>();
            listCulture.AddRange(CultureInfo.GetCultures(CultureTypes.NeutralCultures));
            InvariantCulture = listCulture[0];
            listCulture.Remove(InvariantCulture);
            listCulture.Sort((a, b) => string.Compare(a.EnglishName, b.EnglishName, StringComparison.Ordinal));
            listCulture.Insert(0, InvariantCulture);
            return listCulture;
        }
        public static void ComboBoxPrefLanguageKeyPress(object sender, KeyPressEventArgs e)
        {
            var cb = (ComboBox) sender;
            cb.DroppedDown = true;
            var text = cb.Text;
            if (cb.Tag is bool && ((bool) cb.Tag))
            {
                // Workaround for cb.Text not empty even when we've just set it to ""
                cb.Tag = null;
                text = "";
            }
            switch (e.KeyChar)
            {
                case (char) Keys.Back:
                {
                    if (cb.SelectionStart <= 1)
                    {
                        cb.Text = "";
                        cb.Tag = true; // cleared - see workaround comment above
                        e.Handled = true;
                        return;
                    }
                    var str = cb.Text.Substring(0, cb.SelectionLength > 0 ? cb.SelectionStart - 1 : cb.Text.Length - 1);
                    cb.SelectionStart = str.Length;
                    cb.SelectionLength = cb.Text.Length;
                    e.Handled = true;
                    return;
                }
                case (char) Keys.Enter:
                case (char) Keys.Escape:
                    if (text == "")
                    {
                        cb.Text = "";
                        cb.Text = cb.Items[cb.SelectedIndex].ToString();
                    }
                    return;
            }
            var s = text + e.KeyChar;
            if (cb.SelectionLength > 0)
            {
                s = text.Substring(0, cb.SelectionStart) + e.KeyChar;
            }
            e.Handled = true;
            UpdateSelection(cb, s);
        }

        private static void UpdateSelection(ComboBox cb, string s)
        {
            var i = cb.FindString(s);
            if (i != -1)
            {
                cb.SelectedText = "";
                cb.SelectedIndex = i;
                cb.SelectionStart = s.Length;
                cb.SelectionLength = cb.Text.Length;
            }
        }

        public static void ComboBoxPrefLanguageKeyDown(object sender, KeyEventArgs e)
        {
            var cb = (ComboBox) sender;
            var text = cb.Text;
            if (cb.Tag is bool && ((bool) cb.Tag))
            {
                text = "";
            }
            if (e.KeyCode == Keys.Delete)
            {
                cb.DroppedDown = true;
                e.Handled = true;
                if (cb.SelectedText != text)
                    return;

                cb.DroppedDown = true;
                cb.Text = "";
                cb.Tag = true;
            }
            else
            {
                switch (e.KeyData)
                {
                    case (Keys) Shortcut.CtrlV:
                        if (text == "")
                        {
                            UpdateSelection(cb, Clipboard.GetText());
                        }
                        e.Handled = true;
                        break;
                    case (Keys) Shortcut.CtrlC:
                    {
                        Clipboard.SetData(DataFormats.Text, text);
                        break;
                    }
                }
            }
        }

        public static void ComboBoxPrefLanguageDropDownClosed(object sender, EventArgs e)
        {
            var cb = (ComboBox) sender;
            var i = cb.SelectedIndex;
            cb.SelectedIndex = -1;
            cb.SelectedIndex = i;
            if (cb.Tag is bool && ((bool) cb.Tag))
            {
                cb.Tag = null;
            }
            UpdateSelection(cb, cb.Text);
        }
    }
}