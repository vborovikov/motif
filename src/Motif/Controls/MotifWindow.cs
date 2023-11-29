namespace Motif.Controls
{
	using System.Windows;

	public class MotifWindow : CustomWindow
	{
		static MotifWindow()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MotifWindow), new FrameworkPropertyMetadata(typeof(MotifWindow)));
			RegisterSystemCommands(typeof(MotifWindow));
		}
	}
}
