﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.Core.Presentation;
using ICSharpCode.TreeView;

namespace ICSharpCode.SharpDevelop.Dom.ClassBrowser
{
	public class AssemblyTreeNode : ModelCollectionTreeNode
	{
		IAssemblyModel model;
		
		public AssemblyTreeNode(IAssemblyModel model)
		{
			if (model == null)
				throw new ArgumentNullException("model");
			this.model = model;
		}
		
		protected override object GetModel()
		{
			return model;
		}
		
		protected override IComparer<SharpTreeNode> NodeComparer {
			get {
				return NodeTextComparer;
			}
		}
		
		protected override IModelCollection<object> ModelChildren {
			get {
				return model.Namespaces;
			}
		}
		
		public override object Text {
			get {
				return model.AssemblyName;
			}
		}
		
		public override object Icon {
			get {
				return SD.ResourceService.GetImageSource("Icons.16x16.Assembly");
			}
		}
		
		public override void ShowContextMenu()
		{
			var assemblyModel = this.Model as IAssemblyModel;
			if (assemblyModel != null) {
				var ctx = MenuService.ShowContextMenu(null, assemblyModel, "/SharpDevelop/Pads/ClassBrowser/AssemblyContextMenu");
			}
		}
	}
}


