using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

using Prism.Commands;

using Visualization.Controls.Data;

namespace Visualization.Controls
{
    public sealed class HierarchicalDataCommands
    {
        private readonly Dictionary<MenuItem, Action<HierarchicalData>> _menuItemToAction = new Dictionary<MenuItem, Action<HierarchicalData>>();

        public bool Fill(ContextMenu menu, HierarchicalData data)
        {
            if (!_menuItemToAction.Any())
            {
                return false;
            }

            foreach (var pair in _menuItemToAction)
            {
                var menuItem = pair.Key;

                // Detach context menu items from previous shown context menu (if any)
                var parent = menuItem.Parent as ContextMenu;
                parent?.Items.Clear();
                menuItem.IsEnabled = data != null && data.IsLeafNode;
                menuItem.Command = new DelegateCommand(() => OnMenuClick(menuItem, data));
                menu.Items.Add(menuItem);
            }

            return true;
        }

        public void Register(string title, Action<HierarchicalData> action)
        {
            var item = new MenuItem { Header = title };

            _menuItemToAction[item] = action;
        }

        private void OnMenuClick(MenuItem item, HierarchicalData data)
        {
            // Invoke subscriber with clicked data item.
            _menuItemToAction[item].Invoke(data);
        }
    }
}