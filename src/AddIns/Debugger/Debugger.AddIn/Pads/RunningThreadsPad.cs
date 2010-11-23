﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the BSD license (for details please see \src\AddIns\Debugger\Debugger.AddIn\license.txt)

using System;
using System.Dynamic;
using System.Threading;
using System.Windows;
using System.Windows.Data;

using Debugger;
using Debugger.AddIn.Pads.Controls;
using Debugger.AddIn.TreeModel;
using ICSharpCode.Core;
using Exception = System.Exception;
using Thread = Debugger.Thread;

namespace ICSharpCode.SharpDevelop.Gui.Pads
{
	public partial class RunningThreadsPad : DebuggerPad
	{
		SimpleListViewControl  runningThreadsList;
		Process debuggedProcess;
		
		public override object Control {
			get {
				return runningThreadsList;
			}
		}
		
		protected override void InitializeComponents()
		{
			runningThreadsList = new SimpleListViewControl();
			runningThreadsList.ContextMenu = CreateContextMenuStrip();
			runningThreadsList.ItemActivated += RunningThreadsListItemActivate;
			
			RedrawContent();
			ResourceService.LanguageChanged += delegate { RedrawContent(); };
		}
		
		public void RedrawContent()
		{
			runningThreadsList.ClearColumns();
			runningThreadsList.AddColumn(ResourceService.GetString("Global.ID"),
			                             new Binding { Path = new PropertyPath("ID") },
			                             100);
			runningThreadsList.AddColumn(ResourceService.GetString("Global.Name"),
			                             new Binding { Path = new PropertyPath("Name") },
			                             300);
			runningThreadsList.AddColumn(ResourceService.GetString("AddIns.HtmlHelp2.Location"),
			                             new Binding { Path = new PropertyPath("Location") },
			                             250);
			runningThreadsList.AddColumn(ResourceService.GetString("MainWindow.Windows.Debug.Threads.Priority"),
			                             new Binding { Path = new PropertyPath("Priority") },
			                             120);
			runningThreadsList.AddColumn(ResourceService.GetString("MainWindow.Windows.Debug.Threads.Frozen"),
			                             new Binding { Path = new PropertyPath("Frozen") },
			                             80);
		}

		protected override void SelectProcess(Process process)
		{
			if (debuggedProcess != null) {
				debuggedProcess.Paused               -= debuggedProcess_Paused;
				debuggedProcess.Threads.Added        -= debuggedProcess_ThreadStarted;
			}
			debuggedProcess = process;
			if (debuggedProcess != null) {
				debuggedProcess.Paused               += debuggedProcess_Paused;
				debuggedProcess.Threads.Added        += debuggedProcess_ThreadStarted;
			}
			runningThreadsList.ItemCollection.Clear();
			RefreshPad();
		}
		
		void debuggedProcess_Paused(object sender, ProcessEventArgs e)
		{
			RefreshPad();
		}
		
		void debuggedProcess_ThreadStarted(object sender, CollectionItemEventArgs<Thread> e)
		{
			AddThread(e.Item);
		}
		
		public override void RefreshPad()
		{
			if (debuggedProcess == null || debuggedProcess.IsRunning) {
				runningThreadsList.ItemCollection.Clear();
				return;
			}
			
			using(new PrintTimes("Threads refresh")) {
				try {
					foreach (Thread t in debuggedProcess.Threads) {
						if (debuggedProcess.IsPaused) {
							Utils.DoEvents(debuggedProcess);
						}
						AddThread(t);
					}
				} catch(AbortedBecauseDebuggeeResumedException) {
				} catch(Exception) {
					if (debuggedProcess == null || debuggedProcess.HasExited) {
						// Process unexpectedly exited
					} else {
						throw;
					}
				}
			}
		}

		void RunningThreadsListItemActivate(object sender, EventArgs e)
		{
			if (debuggedProcess.IsPaused) {
				if (debuggedProcess != null) {
					dynamic obj = runningThreadsList.SelectedItems[0];
					debuggedProcess.SelectedThread = (Thread)(obj.Tag);
					debuggedProcess.OnPaused(); // Force refresh of pads
				}
			} else {
				MessageService.ShowMessage("${res:MainWindow.Windows.Debug.Threads.CannotSwitchWhileRunning}", "${res:MainWindow.Windows.Debug.Threads.ThreadSwitch}");
			}
		}
		
		void AddThread(Thread thread)
		{
			// remove the object if exists
			RemoveThread(thread);
			
			dynamic obj = new ExpandoObject();
			obj.Tag = thread;
			RefreshItem(obj);
			runningThreadsList.ItemCollection.Add(obj);
			thread.NameChanged += delegate {
				RefreshItem(obj);
			};
			thread.Exited += delegate {
				RemoveThread(obj);
			};
		}
		
		void RefreshItem(ExpandoObject obj)
		{
			dynamic item = obj;
			var thread = item.Tag as Thread;
			
			item.ID = thread.ID;
			item.Tag = thread;
			StackFrame location = null;
			if (thread.Process.IsPaused) {
				location = thread.MostRecentStackFrame;
			}
			if (location != null) {
				item.Location = location.MethodInfo.Name;
			} else {
				item.Location = ResourceService.GetString("Global.NA");
			}
			
			switch (thread.Priority) {
				case ThreadPriority.Highest:
					item.Priority = ResourceService.GetString("MainWindow.Windows.Debug.Threads.Priority.Highest");
					break;
				case ThreadPriority.AboveNormal:
					item.Priority = ResourceService.GetString("MainWindow.Windows.Debug.Threads.Priority.AboveNormal");
					break;
				case ThreadPriority.Normal:
					item.Priority = ResourceService.GetString("MainWindow.Windows.Debug.Threads.Priority.Normal");
					break;
				case ThreadPriority.BelowNormal:
					item.Priority = ResourceService.GetString("MainWindow.Windows.Debug.Threads.Priority.BelowNormal");
					break;
				case ThreadPriority.Lowest:
					item.Priority = ResourceService.GetString("MainWindow.Windows.Debug.Threads.Priority.Lowest");
					break;
				default:
					item.Priority = thread.Priority.ToString();
					break;
			}
			item.Frozen = ResourceService.GetString(thread.Suspended ? "Global.Yes" : "Global.No");
		}
		
		void RemoveThread(Thread thread)
		{
			foreach (dynamic item in runningThreadsList.ItemCollection) {
				if (thread.ID == item.ID) {
					runningThreadsList.ItemCollection.Remove(item);
					break;
				}
			}
		}
	}
}