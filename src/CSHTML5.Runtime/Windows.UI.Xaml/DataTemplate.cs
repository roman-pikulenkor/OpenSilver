﻿
//===============================================================================
//
//  IMPORTANT NOTICE, PLEASE READ CAREFULLY:
//
//  => This code is licensed under the GNU General Public License (GPL v3). A copy of the license is available at:
//        https://www.gnu.org/licenses/gpl.txt
//
//  => As stated in the license text linked above, "The GNU General Public License does not permit incorporating your program into proprietary programs". It also does not permit incorporating this code into non-GPL-licensed code (such as MIT-licensed code) in such a way that results in a non-GPL-licensed work (please refer to the license text for the precise terms).
//
//  => Licenses that permit proprietary use are available at:
//        http://www.cshtml5.com
//
//  => Copyright 2019 Userware/CSHTML5. This code is part of the CSHTML5 product (cshtml5.com).
//
//===============================================================================



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !MIGRATION
using Windows.UI.Xaml.Controls;
#endif

#if MIGRATION
namespace System.Windows
#else
namespace Windows.UI.Xaml
#endif
{
    /// <summary>
    /// Describes the visual structure of a data object.
    /// </summary>
    public class DataTemplate : FrameworkTemplate
    {
        /// <summary>
        /// Initializes a new instance of the DataTemplate class.
        /// </summary>
        public DataTemplate() : base() { }

        /// <summary>
        /// Creates the System.Windows.UIElement objects in the System.Windows.DataTemplate.
        /// </summary>
        /// <returns>The root System.Windows.UIElement of the System.Windows.DataTemplate.</returns>
        public DependencyObject LoadContent()
        {
            return this.INTERNAL_InstantiateFrameworkTemplate();
        }
    }
}
