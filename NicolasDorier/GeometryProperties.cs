using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NicolasDorier
{
	public class GeometryProperties
	{
		public static string GetData(DependencyObject obj)
		{
			return (string)obj.GetValue(DataProperty);
		}

		public static void SetData(DependencyObject obj, string value)
		{
			obj.SetValue(DataProperty, value);
		}

		// Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty DataProperty =
			DependencyProperty.RegisterAttached("Data", typeof(string), typeof(GeometryProperties), new PropertyMetadata(null, OnDataChanged));

		static void OnDataChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
		{
			var path = source as Path;
			GeometryGroup pathGeometry = source as GeometryGroup;
			if(path != null)
			{
				pathGeometry = new GeometryGroup();
				path.Data = pathGeometry;
			}

			if(pathGeometry != null)
			{
				var processing = new ProcessingContext(pathGeometry);
				processing.Execute(args.NewValue as string);
			}
		}

	}
}
