namespace Motif.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    [TemplatePart(Name = TemplateParts.Icon, Type = typeof(Image))]
	[TemplatePart(Name = TemplateParts.ClientAreaBorder, Type = typeof(FrameworkElement))]
	public abstract class CustomWindow : Window
	{
		private static class TemplateParts
		{
			internal const string Icon = "PART_Icon";
			internal const string ClientAreaBorder = "PART_ClientAreaBorder";
		}

		private FrameworkElement clientAreaBorder;
		private Image icon;

		protected static void RegisterSystemCommands(Type forType)
		{
			CommandManager.RegisterClassCommandBinding(forType,
				new CommandBinding(SystemCommands.MinimizeWindowCommand, MinimizeWindowExecuted, MinimizeWindowCanExecute));
			CommandManager.RegisterClassCommandBinding(forType,
				new CommandBinding(SystemCommands.MaximizeWindowCommand, MaximizeWindowExecuted, MaximizeWindowCanExecute));
			CommandManager.RegisterClassCommandBinding(forType,
				new CommandBinding(SystemCommands.RestoreWindowCommand, RestoreWindowExecuted, RestoreWindowCanExecute));
			CommandManager.RegisterClassCommandBinding(forType,
				new CommandBinding(SystemCommands.CloseWindowCommand, CloseWindowExecuted, CloseWindowCanExecute));
			CommandManager.RegisterClassCommandBinding(forType,
				new CommandBinding(SystemCommands.ShowSystemMenuCommand, SystemMenuExecuted, SystemMenuCanExecute));
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			this.icon = base.GetTemplateChild(TemplateParts.Icon) as Image;
			if (this.icon != null)
			{
				this.icon.MouseLeftButtonDown += new MouseButtonEventHandler(this.IconMouseLeftButtonDown);
				this.icon.MouseRightButtonDown += new MouseButtonEventHandler(this.IconMouseRightButtonDown);
			}
			this.clientAreaBorder = base.GetTemplateChild(TemplateParts.ClientAreaBorder) as FrameworkElement;
		}

		private void IconMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 1)
			{
				if (SystemCommands.ShowSystemMenuCommand.CanExecute(null, this))
				{
					SystemCommands.ShowSystemMenuCommand.Execute(null, this);
				}
			}
			else if ((e.ClickCount == 2) && SystemCommands.CloseWindowCommand.CanExecute(null, this))
			{
				SystemCommands.CloseWindowCommand.Execute(null, this);
			}
		}

		private void IconMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (SystemCommands.ShowSystemMenuCommand.CanExecute(e, this))
			{
				SystemCommands.ShowSystemMenuCommand.Execute(e, this);
			}
		}

		private static void CloseWindowCanExecute(object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = true;
		}

		private static void CloseWindowExecuted(object sender, ExecutedRoutedEventArgs args)
		{
            if (sender is CustomWindow window)
            {
                SystemCommands.CloseWindow(window);
                args.Handled = true;
            }
        }

		private static void MaximizeWindowCanExecute(object sender, CanExecuteRoutedEventArgs args)
		{
            if (sender is CustomWindow window &&
                window.WindowState != WindowState.Maximized &&
                window.ResizeMode != ResizeMode.NoResize &&
                window.ResizeMode != ResizeMode.CanMinimize)
            {
                args.CanExecute = true;
            }
        }

		private static void MaximizeWindowExecuted(object sender, ExecutedRoutedEventArgs args)
		{
            if (sender is CustomWindow window)
            {
                SystemCommands.MaximizeWindow(window);
                args.Handled = true;
            }
        }

		private static void MinimizeWindowCanExecute(object sender, CanExecuteRoutedEventArgs args)
		{
            if (sender is CustomWindow window &&
                window.WindowState != WindowState.Minimized &&
                window.ResizeMode != ResizeMode.NoResize)
            {
                args.CanExecute = true;
            }
        }

		private static void MinimizeWindowExecuted(object sender, ExecutedRoutedEventArgs args)
		{
            if (sender is CustomWindow window)
            {
                SystemCommands.MinimizeWindow(window);
                args.Handled = true;
            }
        }

		private static void RestoreWindowCanExecute(object sender, CanExecuteRoutedEventArgs args)
		{
            if ((sender is CustomWindow window) && (window.WindowState != WindowState.Normal))
            {
                args.CanExecute = true;
            }
        }

		private static void RestoreWindowExecuted(object sender, ExecutedRoutedEventArgs args)
		{
            if (sender is CustomWindow window)
            {
                SystemCommands.RestoreWindow(window);
                args.Handled = true;
            }
        }

		private static void SystemMenuCanExecute(object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = true;
		}

		private static void SystemMenuExecuted(object sender, ExecutedRoutedEventArgs args)
		{
            if (sender is CustomWindow relativeTo)
            {
                Point point;
                if (args.Parameter is MouseButtonEventArgs parameter)
                {
                    point = relativeTo.PointToScreen(parameter.GetPosition(relativeTo));
                }
                else if (relativeTo.clientAreaBorder != null)
                {
                    point = relativeTo.clientAreaBorder.PointToScreen(new Point(0.0, 0.0));
                }
                else
                {
                    return;
                }
                var compositionTarget = PresentationSource.FromVisual(relativeTo).CompositionTarget;
                SystemCommands.ShowSystemMenu(relativeTo, compositionTarget.TransformFromDevice.Transform(point));
                args.Handled = true;
            }
        }
	}
}
