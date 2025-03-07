//------------------------------------------------------------------------------
// <copyright file="ToolStripDesignerUtils.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Windows.Forms.Design {
    using System;
    using System.Windows.Forms;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Collections;
    using System.Drawing;
    using System.Drawing.Design;
    using System.Collections.Generic;
    using Microsoft.Win32;
    using System.Security;
    using System.Security.Permissions;
    using System.Reflection;
    using System.Windows.Forms.Design.Behavior;


    internal sealed class ToolStripDesignerUtils {

        private static Type toolStripItemType = typeof(System.Windows.Forms.ToolStripItem);
        [ThreadStatic]
        private static Dictionary<Type, ToolboxItem> CachedToolboxItems;
        [ThreadStatic]
        private static int CustomToolStripItemCount = 0;
        private const int TOOLSTRIPCHARCOUNT = 9;
        // used to cache in the selection.
        // This is used when the selection is changing and we need to invalidate the original selection
        // Especially, when the selection is changed through NON UI designer action like through propertyGrid or through Doc Outline.
        public static ArrayList originalSelComps;
        
        
        [ThreadStatic]
        private static Dictionary<Type, Bitmap> CachedWinformsImages;

        private static string systemWindowsFormsNamespace = typeof(System.Windows.Forms.ToolStripItem).Namespace;
      
        private ToolStripDesignerUtils() {
        }
    
#region NewItemTypeLists
        // This section controls the ordering of standard item types in all the various pieces
        // of toolstrip designer UI.

        // ToolStrip - Default item is determined by being first in the list
        private static readonly Type[] NewItemTypesForToolStrip =     new Type[]{typeof(ToolStripButton),
                                                                                 typeof(ToolStripLabel),
                                                                                 typeof(ToolStripSplitButton),
                                                                                 typeof(ToolStripDropDownButton),
                                                                                 typeof(ToolStripSeparator),
                                                                                 typeof(ToolStripComboBox),
                                                                                 typeof(ToolStripTextBox),
                                                                                 typeof(ToolStripProgressBar)};

        // StatusStrip - Default item is determined by being first in the list
        private static readonly Type[] NewItemTypesForStatusStrip =   new Type[]{typeof(ToolStripStatusLabel),            
                                                                                 typeof(ToolStripProgressBar),
                                                                                 typeof(ToolStripDropDownButton),
                                                                                 typeof(ToolStripSplitButton)};

        
        // MenuStrip - Default item is determined by being first in the list
        private static readonly Type[] NewItemTypesForMenuStrip =    new Type[]{typeof(ToolStripMenuItem),
                                                                                typeof(ToolStripComboBox),
                                                                                typeof(ToolStripTextBox)};
        // ToolStripDropDown - roughly same as menu strip.
        private static readonly Type[] NewItemTypesForToolStripDropDownMenu = new Type[]{typeof(ToolStripMenuItem),
                                                                                         typeof(ToolStripComboBox),
                                                                                         typeof(ToolStripSeparator),
                                                                                         typeof(ToolStripTextBox)};
#endregion



        // Get the Correct bounds for painting...
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        public static void GetAdjustedBounds(ToolStripItem item, ref Rectangle r)
        {
            // adjust bounds as per item
            if (!(item is ToolStripControlHost && item.IsOnDropDown))
            {
                //Deflate the SelectionGlyph for the MenuItems...
                if (item is ToolStripMenuItem && item.IsOnDropDown)
                {
                    r.Inflate(-3, -2);
                    r.Width++;
                }
                else if (item is ToolStripControlHost && !item.IsOnDropDown)
                {
                    r.Inflate(0, -2);
                }
                else if (item is ToolStripMenuItem && !item.IsOnDropDown)
                {
                    r.Inflate(-3, -3);
                }
                else
                {
                    r.Inflate(-1, -1);
                }
            }
        }
                
        /// <devdoc>
        /// If IComponent is ToolStrip return ToolStrip
        /// If IComponent is ToolStripItem return the Owner ToolStrip
        /// If IComponent is ToolStripDropDownItem return the child DropDown ToolStrip
        /// </devdoc>
        private static ToolStrip GetToolStripFromComponent(IComponent component) {
            ToolStripItem stripItem = component as ToolStripItem;
            ToolStrip parent = null;
     
            if (stripItem != null) {
                if (!(stripItem is ToolStripDropDownItem)){
                    parent = stripItem.Owner;
                }
                else {
                    parent = ((ToolStripDropDownItem)stripItem).DropDown;
                }
                
            }
            else {
                parent = component as ToolStrip;
            }
            return parent;
      
        }

        private static ToolboxItem GetCachedToolboxItem(Type itemType) {
            
            ToolboxItem tbxItem  = null;
            if (CachedToolboxItems == null){
                CachedToolboxItems = new Dictionary<Type,ToolboxItem>();
            }
            else if (CachedToolboxItems.ContainsKey(itemType)){
                tbxItem = CachedToolboxItems[itemType];
                return tbxItem;
            }

            // no cache hit - load the item.
           
            if (tbxItem == null) {
                tbxItem = ToolboxService.GetToolboxItem(itemType);
                if (tbxItem == null) {
                     // create a toolbox item to match
                     tbxItem = new ToolboxItem(itemType);
                }
            }

            CachedToolboxItems[itemType] = tbxItem;
             
            if (CustomToolStripItemCount > 0 && (CustomToolStripItemCount*2 < CachedToolboxItems.Count)) {
                // time to clear the toolbox item cache - we've got twice the number of toolbox items than
                // actual custom item types.
                CachedToolboxItems.Clear();
            }
            return tbxItem;
        }

        // only call this for well known items.
        private static Bitmap GetKnownToolboxBitmap(Type itemType) {
             if (CachedWinformsImages == null) {
                 CachedWinformsImages = new Dictionary<Type, Bitmap>();
             }
             if (!CachedWinformsImages.ContainsKey(itemType)) {
                 Bitmap knownImage = ToolboxBitmapAttribute.GetImageFromResource(itemType, null, false) as Bitmap;
                 CachedWinformsImages[itemType] = knownImage;
                 return knownImage;             
             }
             return CachedWinformsImages[itemType];
        }

        public static Bitmap GetToolboxBitmap(Type itemType) {
            
            // if and only if the namespace of the type is System.Windows.Forms.
            // try to pull the image out of the manifest.
            if (itemType.Namespace == systemWindowsFormsNamespace) {
               return GetKnownToolboxBitmap(itemType);
            }

            // check to see if we've got a toolbox item, and use it.
            ToolboxItem tbxItem = GetCachedToolboxItem(itemType);
            if (tbxItem != null) {
                return tbxItem.Bitmap;
            }

            // if all else fails, throw up a default image.
            return GetKnownToolboxBitmap(typeof(Component));
                
        }

        /// <devdoc>
        /// Fishes out the display name attribute from the Toolbox item 
        /// if not present, uses Type.Name
        /// </devdoc>
        public static string GetToolboxDescription(Type itemType) {
            String currentName = null;
            
            ToolboxItem tbxItem = GetCachedToolboxItem(itemType);
            if (tbxItem != null) {
                currentName = tbxItem.DisplayName;
            }
            if (currentName == null)
            {
                currentName = itemType.Name;
            }
            if (currentName.StartsWith("ToolStrip"))
            {
                return currentName.Substring(TOOLSTRIPCHARCOUNT);
            }
            return currentName;
        }


        /// <devdoc> 
        /// GetStandardItemTypes
        ///  The first item returned should be the DefaultItem to create on the ToolStrip
        /// </devdoc>
        public static Type[] GetStandardItemTypes(IComponent component) {
            ToolStrip toolStrip = GetToolStripFromComponent(component);
            
            if (toolStrip is MenuStrip) {
                return NewItemTypesForMenuStrip;
            }
            else if (toolStrip is ToolStripDropDownMenu) {
                // ToolStripDropDown gets default items.
                 return NewItemTypesForToolStripDropDownMenu;
            }
            else if (toolStrip is StatusStrip) {
                return NewItemTypesForStatusStrip;
            }
            Debug.Assert(toolStrip != null, "why werent we handed a toolstrip here? returning default list");
            
            return NewItemTypesForToolStrip;
        }

        private static ToolStripItemDesignerAvailability GetDesignerVisibility(ToolStrip toolStrip) {
            ToolStripItemDesignerAvailability visiblity = ToolStripItemDesignerAvailability.None;
            if (toolStrip is StatusStrip) {
               visiblity = ToolStripItemDesignerAvailability.StatusStrip;
            }
            else if (toolStrip is MenuStrip) {
                visiblity = ToolStripItemDesignerAvailability.MenuStrip;
            }
            else if (toolStrip is ToolStripDropDownMenu) {
                visiblity = ToolStripItemDesignerAvailability.ContextMenuStrip;
            }
            else {
                visiblity = ToolStripItemDesignerAvailability.ToolStrip;
            }
            return visiblity;

        }


        public static Type[] GetCustomItemTypes(IComponent component, IServiceProvider serviceProvider) {
            ITypeDiscoveryService discoveryService = null;
            if (serviceProvider != null) {
                discoveryService = serviceProvider.GetService(typeof(ITypeDiscoveryService)) as ITypeDiscoveryService;
            }
            return GetCustomItemTypes(component, discoveryService);

        }
        public static Type[] GetCustomItemTypes(IComponent component, ITypeDiscoveryService discoveryService) {

            if (discoveryService != null) {
        
                 // fish out all types which derive from toolstrip item
                 ICollection itemTypes = discoveryService.GetTypes(toolStripItemType, false /*excludeGlobalTypes*/);
                 ToolStrip toolStrip = GetToolStripFromComponent(component);
                 
                 // determine the value of the visibility attribute which matches the current toolstrip type.
                 ToolStripItemDesignerAvailability currentToolStripVisibility = GetDesignerVisibility(toolStrip);
                 Debug.Assert(currentToolStripVisibility != ToolStripItemDesignerAvailability.None, "Why is GetDesignerVisibility returning None?");

                 // fish out the ones we've already listed.
                 Type[] stockItemTypeList = GetStandardItemTypes(component);
                
                 
                 if (currentToolStripVisibility != ToolStripItemDesignerAvailability.None) {
                     ArrayList createableTypes = new ArrayList(itemTypes.Count);
                
                     foreach (Type t in itemTypes) {
                         if (t.IsAbstract){
                             continue;
                         }
                         if (!t.IsPublic && !t.IsNestedPublic){
                             continue;
                         }
                         if (t.ContainsGenericParameters) {
                             continue;
                         }

                         // Check if we have public constructor...
                         ConstructorInfo ctor = t.GetConstructor(new Type[0]);
                         if (ctor == null)
                         {
                            continue;
                         }

                         // if the visibility matches the current toolstrip type, 
                         // add it to the list of possible types to create.
                         ToolStripItemDesignerAvailabilityAttribute visiblityAttribute = (ToolStripItemDesignerAvailabilityAttribute)TypeDescriptor.GetAttributes(t)[typeof(ToolStripItemDesignerAvailabilityAttribute)];
                         if (visiblityAttribute != null && ((visiblityAttribute.ItemAdditionVisibility & currentToolStripVisibility) == currentToolStripVisibility)) {
                            bool isStockType = false;
                            // PERF: consider a dictionary - but this list will usually be 3-7 items.
                            foreach (Type stockType in stockItemTypeList) {
                                if (stockType == t) {
                                    isStockType = true;
                                    break;
                                }
                            }
                            if (!isStockType) {
                                createableTypes.Add(t);
                            }
                         }
                     }

                     if (createableTypes.Count > 0) {
                         Type[] createableTypesArray = new Type[createableTypes.Count];
                         createableTypes.CopyTo(createableTypesArray,0);
                         CustomToolStripItemCount = createableTypes.Count;
                         return createableTypesArray;
                     }
                }
             
            }
            CustomToolStripItemCount = 0;
            return new Type[0];
        }


        /// <devdoc> 
        /// GetStandardItemMenuItems
        ///  wraps the result of GetStandardItemTypes in ItemTypeToolStripMenuItems.
        /// </devdoc>
        public static ToolStripItem[] GetStandardItemMenuItems(IComponent component, EventHandler onClick, bool convertTo) {
            Type[] standardTypes = GetStandardItemTypes(component);
            ToolStripItem[] items = new ToolStripItem[standardTypes.Length];
            for ( int i = 0; i <standardTypes.Length; i++) {
               ItemTypeToolStripMenuItem item = new ItemTypeToolStripMenuItem(standardTypes[i]);
               item.ConvertTo = convertTo;
               if (onClick != null) {
                 item.Click += onClick;
               }
               items[i] = item;
            }
            
            return items;
        }

        
        /// <devdoc> 
        /// GetCustomItemMenuItems
        ///  wraps the result of GetCustomItemTypes in ItemTypeToolStripMenuItems.
        /// </devdoc>
        public static ToolStripItem[] GetCustomItemMenuItems(IComponent component, EventHandler onClick, bool convertTo, IServiceProvider serviceProvider) {
            Type[] customTypes = GetCustomItemTypes(component, serviceProvider);
            
            ToolStripItem[] items = new ToolStripItem[customTypes.Length];
            for ( int i = 0; i <customTypes.Length; i++) {
               ItemTypeToolStripMenuItem item = new ItemTypeToolStripMenuItem(customTypes[i]);
               item.ConvertTo = convertTo;
               if (onClick != null) {
                  item.Click += onClick;
               }
               items[i] = item;
            }
            return items;
     
        }

        /// <devdoc>
        /// GetNewItemDropDown
        /// build up a list of standard items separated by the custom items
        /// </devdoc>

        public static NewItemsContextMenuStrip GetNewItemDropDown(IComponent component, ToolStripItem currentItem, EventHandler onClick, bool convertTo, IServiceProvider serviceProvider, bool populateCustom) {
 
            NewItemsContextMenuStrip contextMenu = new NewItemsContextMenuStrip(component, currentItem, onClick, convertTo, serviceProvider);
            contextMenu.GroupOrdering.Add("StandardList");
            contextMenu.GroupOrdering.Add("CustomList");
            
            // plumb through the standard and custom items.
            foreach (ToolStripItem item in GetStandardItemMenuItems(component, onClick, convertTo)) {
                contextMenu.Groups["StandardList"].Items.Add(item);
                if (convertTo)
                {
                    ItemTypeToolStripMenuItem toolItem = item as ItemTypeToolStripMenuItem;
                    if (toolItem != null && currentItem != null && toolItem.ItemType == currentItem.GetType())
                    {
                        toolItem.Enabled = false;
                    }
                }
                
            }

            if (populateCustom)
            {
                GetCustomNewItemDropDown(contextMenu, component, currentItem, onClick, convertTo, serviceProvider); 
            }

            IUIService uis = serviceProvider.GetService(typeof(IUIService)) as IUIService;
            if (uis != null) 
            {
                contextMenu.Renderer =(ToolStripProfessionalRenderer)uis.Styles["VsRenderer"];
                contextMenu.Font = (Font)uis.Styles["DialogFont"];
                if (uis.Styles["VsColorPanelText"] is Color)
                {
                    contextMenu.ForeColor = (Color)uis.Styles["VsColorPanelText"];
                }
            }
            contextMenu.Populate();
            
            return contextMenu;
        }

        public static void GetCustomNewItemDropDown(NewItemsContextMenuStrip contextMenu, IComponent component, ToolStripItem currentItem, EventHandler onClick, bool convertTo, IServiceProvider serviceProvider)
        {
            foreach (ToolStripItem item in GetCustomItemMenuItems(component, onClick, convertTo, serviceProvider))
            {
                contextMenu.Groups["CustomList"].Items.Add(item);
                if (convertTo)
                {
                    ItemTypeToolStripMenuItem toolItem = item as ItemTypeToolStripMenuItem;
                    if (toolItem != null && currentItem != null && toolItem.ItemType == currentItem.GetType())
                    {
                        toolItem.Enabled = false;
                    }
                }
            }
            contextMenu.Populate();
        }

        public static void InvalidateSelection(ArrayList originalSelComps, ToolStripItem nextSelection, IServiceProvider provider, bool shiftPressed)
        {
            // if we are not selecting a ToolStripItem then return (dont invalidate).
            if (nextSelection == null || provider == null)
            {
                return;
            }
            //InvalidateOriginal SelectedComponents.
            Region invalidateRegion = null;
            Region itemRegion = null;
            int GLYPHBORDER = 1;
            int GLYPHINSET = 2;
            ToolStripItemDesigner designer = null;
            bool templateNodeSelected = false;           
            
            try
            {
                Rectangle invalidateBounds = Rectangle.Empty;
                IDesignerHost designerHost = (IDesignerHost)provider.GetService(typeof(IDesignerHost));
                
                if (designerHost != null)
                {
                    foreach(Component comp in originalSelComps)
                    {
                        ToolStripItem selItem = comp as ToolStripItem;
                        if (selItem != null)
                        {
                            if ((originalSelComps.Count > 1) || 
                               (originalSelComps.Count == 1 && selItem.GetCurrentParent() != nextSelection.GetCurrentParent()) ||
                               selItem is ToolStripSeparator || selItem is ToolStripControlHost || !selItem.IsOnDropDown || selItem.IsOnOverflow)
                            {
                                // finally Invalidate the selection rect ...
                                designer = designerHost.GetDesigner(selItem) as ToolStripItemDesigner;
                                if (designer != null)
                                {   
                                    invalidateBounds = designer.GetGlyphBounds();
                                    GetAdjustedBounds(selItem, ref invalidateBounds);
                                    invalidateBounds.Inflate(GLYPHBORDER, GLYPHBORDER);

                                    if (invalidateRegion == null)
                                    {
                                        invalidateRegion = new Region(invalidateBounds);
                                        invalidateBounds.Inflate(-GLYPHINSET, -GLYPHINSET);
                                        invalidateRegion.Exclude(invalidateBounds);
                                    }
                                    else
                                    {
                                        itemRegion = new Region(invalidateBounds);
                                        invalidateBounds.Inflate(-GLYPHINSET, -GLYPHINSET);
                                        itemRegion.Exclude(invalidateBounds);
                                        invalidateRegion.Union(itemRegion);
                                    }
                                }
                                else if (selItem is DesignerToolStripControlHost)
                                {
                                    templateNodeSelected = true;
                                }
                            }
                        }
                    }
                }
                
                if (invalidateRegion != null || templateNodeSelected || shiftPressed)
                {   
                    BehaviorService behaviorService = (BehaviorService)provider.GetService(typeof(BehaviorService));

                    if (behaviorService != null)
                    {
                        if (invalidateRegion != null)
                        {
                            behaviorService.Invalidate(invalidateRegion);
                        }

                        // When a ToolStripItem is PrimarySelection, the glyph bounds are not invalidated 
                        // through the SelectionManager so we have to do this.
                        designer = designerHost.GetDesigner(nextSelection) as ToolStripItemDesigner;
                        if (designer != null)
                        {
                            invalidateBounds = designer.GetGlyphBounds();
                            GetAdjustedBounds(nextSelection, ref invalidateBounds);
                            invalidateBounds.Inflate(GLYPHBORDER, GLYPHBORDER);
                            invalidateRegion = new Region(invalidateBounds);

                            invalidateBounds.Inflate(-GLYPHINSET, -GLYPHINSET);
                            invalidateRegion.Exclude(invalidateBounds);
                            behaviorService.Invalidate(invalidateRegion);
                        }
                    }
                }
            }
            finally
            {
                if (invalidateRegion != null)
                {
                    invalidateRegion.Dispose();
                }

                if (itemRegion != null)
                {
                    itemRegion.Dispose();
                }
            }
        }

        /// <devdoc> represents cached information about the display</devdoc>
        internal static class DisplayInformation {
               private static bool highContrast;  //whether we are under hight contrast mode
               private static bool lowRes;  //whether we are under low resolution mode
               private static bool isTerminalServerSession;   //whether this application is run on a terminal server (remote desktop)
               private static bool highContrastSettingValid;   //indicates whether the high contrast setting is correct
               private static bool lowResSettingValid;  //indicates whether the low resolution setting is correct
               private static bool terminalSettingValid; //indicates whether the terminal server setting is correct
               private static short bitsPerPixel;
               private static bool dropShadowSettingValid;
               private static bool dropShadowEnabled;
           
           
               static DisplayInformation() {
                   SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);
                   SystemEvents.DisplaySettingsChanged += new EventHandler(DisplaySettingChanged); 
               }
       
               public static short BitsPerPixel {
                  get {
                      if (bitsPerPixel == 0) {
                          new EnvironmentPermission(PermissionState.Unrestricted).Assert();
                          try {
                              foreach (Screen s in Screen.AllScreens) {
                                  if (bitsPerPixel == 0) {
                                      bitsPerPixel = (short)s.BitsPerPixel;
                                  }
                                  else {
                                      bitsPerPixel = (short)Math.Min(s.BitsPerPixel, bitsPerPixel);
                                  }
                              }
                          }
                          finally {
                              CodeAccessPermission.RevertAssert();
                          }
                      }
                      return bitsPerPixel;
                  }
              }
       
               ///<devdoc>
               ///tests to see if the monitor is in low resolution mode (8-bit color depth or less).
               ///</devdoc>
               public static bool LowResolution {
                   get {
                       if (lowResSettingValid) {
                           return lowRes;
                       }
       
                       lowRes = BitsPerPixel <= 8;
                       lowResSettingValid = true;
                       return lowRes;
                   }
               }
       
               ///<devdoc>
               ///tests to see if we are under high contrast mode
               ///</devdoc>
               public static bool HighContrast {
                   get {
                       if (highContrastSettingValid) {
                           return highContrast;
                       }
                       highContrast = SystemInformation.HighContrast;
                       highContrastSettingValid = true;
                       return highContrast;
                   }
               }
               public static bool IsDropShadowEnabled {
                   [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                   get {
                       if (dropShadowSettingValid) {
                           return dropShadowEnabled;
                       }
                       dropShadowEnabled = SystemInformation.IsDropShadowEnabled;
                       dropShadowSettingValid = true;
                       return dropShadowEnabled;
                   }
               }
       
               ///<devdoc>
               ///test to see if we are under terminal server mode
               ///</devdoc>
               public static bool TerminalServer {
                   get {
                       if (terminalSettingValid) {
                           return isTerminalServerSession;
                       }
       
                       isTerminalServerSession = SystemInformation.TerminalServerSession;
                       terminalSettingValid = true;
                       return isTerminalServerSession;
                   }
               }
       
               ///<devdoc>
               ///event handler for change in display setting
               ///</devdoc>
               private static void DisplaySettingChanged(object obj, EventArgs ea)
               {    
                   highContrastSettingValid = false;
                   lowResSettingValid = false;
                   terminalSettingValid = false;
                   dropShadowSettingValid = false;
               }
       
               ///<devdoc>
               ///event handler for change in user preference
               ///</devdoc>
               private static void UserPreferenceChanged(object obj, UserPreferenceChangedEventArgs ea) {
                   highContrastSettingValid = false;
                   lowResSettingValid = false;
                   terminalSettingValid = false;
                   dropShadowSettingValid = false;
                   bitsPerPixel = 0;
               }
           }
       }
}

