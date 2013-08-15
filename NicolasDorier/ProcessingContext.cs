using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace NicolasDorier
{

	public class ProcessingContext
	{
		private PathFigure _CurrentFigure;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private PathFigure CurrentFigure
		{
			get
			{
				if(_CurrentFigure == null)
					NewFigure();
				return _CurrentFigure;
			}
		}

		private PathGeometry _Path;

		private GeometryGroup _Geometries;

		public ProcessingContext(GeometryGroup group)
		{
			_Path = new PathGeometry();
			_Geometries = group;
			_Geometries.Children.Add(_Path);
		}

		Point _Origin = new Point(0.0, 0.0);
		Transform _CurrentTransform = Transform.Identity;

		public Point Origin
		{
			get
			{
				return _Origin;
			}
		}

		[GeometryCommand("ROT")]
		public void Rotate(double degree)
		{

			var rotation = new RotateTransform(degree)
			{
				CenterX = _Origin.X,
				CenterY = _Origin.Y
			};
			this.PushTransform(rotation);
		}

		private void PushTransform(Transform transform)
		{
			Append(transform.Value);
			_Origin = _CurrentTransform.Transform(new Point(0, 0));
		}

		private void Append(Matrix matrix)
		{
			var currentTransform = _CurrentTransform.Value;
			currentTransform.Append(matrix);
			_CurrentTransform = new MatrixTransform(currentTransform);
		}

		[GeometryCommand("S")]
		public void Scale(double factor)
		{
			this.PushTransform(new ScaleTransform(factor, factor)
			{
				CenterX = _Origin.X,
				CenterY = _Origin.Y
			});
		}
		[GeometryCommand("S")]
		public void Scale(double x, double y)
		{
			this.PushTransform(new ScaleTransform(x, y)
			{
				CenterX = _Origin.X,
				CenterY = _Origin.Y
			});
		}

		Stack<Matrix> _Matrixes = new Stack<Matrix>();


		[GeometryCommand("PUSH")]
		public void PushMatrix()
		{
			_Matrixes.Push(_CurrentTransform.Value);
		}

		[GeometryCommand("POP")]
		public void PopMatrix()
		{
			_CurrentTransform = new MatrixTransform(_Matrixes.Pop());
			_Origin = _CurrentTransform.Transform(new Point(0, 0));
		}

		[GeometryCommand("L")]
		public LineSegment Line(double x)
		{
			return Line(x, 0);
		}

		[GeometryCommand("L")]
		public LineSegment Line(double x, double y)
		{
			return Line(x, y, true);
		}
		[GeometryCommand("L")]
		public LineSegment Line(double x, double y, bool stroked)
		{
			var nextPoint = _CurrentTransform.Transform(new Point(x, y));
			var line = new LineSegment(nextPoint, stroked);
			this.CurrentFigure.Segments.Add(line);
			Translate(x, y);
			return line;
		}
		private void Translate(double x, double y)
		{
			var t = _CurrentTransform.Transform(new Point(x, y));
			PushTransform(new TranslateTransform(t.X - _Origin.X, t.Y - _Origin.Y));
		}

		[GeometryCommand("BASIS")]
		public void Basis(double size)
		{
			var oldFigure = _CurrentFigure;

			_CurrentFigure = new PathFigure();
			_CurrentFigure.IsClosed = false;
			_CurrentFigure.IsFilled = false;
			_Path.Figures.Add(_CurrentFigure);
			PushMatrix();

			Line(-size, 0, false);
			Line(size, 0);
			Ellipse(1);
			Line(size, 0.0);
			DrawArrowHead(size / 5);
			Line(-size, 0.0, false);

			Rotate(90);

			Line(-size, 0, false);
			Line(size, 0);
			Line(size, 0.0);
			DrawArrowHead(size / 10);



			_CurrentFigure = oldFigure;
			PopMatrix();
		}

		private void DrawArrowHead(double arrowSize)
		{
			PushMatrix();
			Rotate(135);
			Line(arrowSize);
			Line(-arrowSize, 0, false);
			PopMatrix();

			PushMatrix();
			Rotate(-135);
			Line(arrowSize);

			Line(-arrowSize, 0, false);
			PopMatrix();


		}




		[GeometryCommand("BEZ")]
		public BezierSegment Bezier(int x, int y, int controlX, int controlY, bool stroked = true)
		{
			var nextPoint = _CurrentTransform.Transform(new Point(x, y));
			var controlPoint = _CurrentTransform.Transform(new Point(controlX, controlY));
			var bezier = new BezierSegment(_Origin, controlPoint, nextPoint, stroked);
			this.CurrentFigure.Segments.Add(bezier);
			Translate(x, y);
			return bezier;
		}


		[GeometryCommand("M")]
		public void Move(double x)
		{
			Translate(x, 0);
		}

		[GeometryCommand("M")]
		public void Move(double x, double y)
		{
			Translate(x, y);
		}





		private void Remove(Matrix matrix)
		{
			matrix.Invert();
			Append(matrix);
		}




		bool _Closed;
		bool _Filled;


		[GeometryCommand("E")]
		public PathGeometry Ellipse(double radius)
		{
			return Ellipse(radius, radius);
		}

		[GeometryCommand("E")]
		public PathGeometry Ellipse(double xRadius, double yRadius)
		{
			var result = ExtractPath(new EllipseGeometry(new Point(0, 0), xRadius, yRadius, _CurrentTransform));
			_Geometries.Children.Add(result);
			return result;
		}


		[GeometryCommand("R")]
		public PathGeometry Rectangle(double radius)
		{
			return Rectangle(radius, radius);
		}

		[GeometryCommand("R")]
		public PathGeometry Rectangle(double xRadius, double yRadius)
		{
			return Rectangle(xRadius, yRadius, 0.0, 0.0);
		}

		[GeometryCommand("R")]
		public PathGeometry Rectangle(double xRadius, double yRadius, double xcornRadius, double ycornRadius)
		{
			var result = ExtractPath(new RectangleGeometry(new Rect(-xRadius, -yRadius, xRadius * 2, yRadius * 2), xcornRadius, ycornRadius, _CurrentTransform));
			_Geometries.Children.Add(result);
			return result;
		}

		private PathGeometry ExtractPath(Geometry geometry)
		{
			var figures = geometry.GetOutlinedPathGeometry().Figures.Select(f =>
			{
				var figure = f.Clone();
				SetProperties(figure);
				return figure;
			});
			return new PathGeometry(figures);
		}



		[GeometryCommand("C")]
		public void Closed()
		{
			_Closed = true;
		}

		[GeometryCommand("F")]
		public void Filled()
		{
			_Filled = true;
		}

		[GeometryCommand("NF")]
		public void NotFilled()
		{
			_Filled = false;
		}

		[GeometryCommand("NC")]
		public void NotClosed()
		{
			_Closed = false;
		}

		[GeometryCommand("Z")]
		public PathFigure EndFigure()
		{
			var figure = CurrentFigure;
			SetProperties(figure);

			_CurrentFigure = null;
			return figure;
		}

		private void SetProperties(PathFigure figure)
		{
			figure.IsClosed = _Closed;
			figure.IsFilled = _Filled;
		}


		private void NewFigure()
		{
			_CurrentFigure = new PathFigure();
			_Path.Figures.Add(_CurrentFigure);
			SetProperties(_CurrentFigure);
			_CurrentFigure.StartPoint = _Origin;
		}

		public void Execute(string data)
		{
			if(string.IsNullOrEmpty(data))
				return;
			var parts = new Queue<string>(Regex.Split(data, @"\s+"));
			List<CandidateMethod> currentOperations = new List<CandidateMethod>();
			CandidateMethod lastCandidate = null;
			int paramNumber = 0;

			while(parts.Count != 0)
			{
				var p = parts.Dequeue();
				paramNumber++;

				foreach(var candidate in currentOperations.ToList())
				{
					try
					{
						var parameter = candidate.Method.GetParameters()[paramNumber - 1];
						candidate.ParameterValues.Add(Convert.ChangeType(p, parameter.ParameterType, CultureInfo.InvariantCulture));
					}
					catch(Exception)
					{
						currentOperations.Remove(candidate);
						if(currentOperations.Count == 0)
							lastCandidate = candidate;
					}
				}

				if(lastCandidate != null)
				{
					lastCandidate.Invoke();
					lastCandidate = null;
				}

				if(currentOperations.Count == 0)
				{
					currentOperations = FindOperations(p);
					if(currentOperations.Count == 0)
						throw new InvalidOperationException("Geometry command : " + p + " is unknown");
					paramNumber = 0;
					continue;
				}
			}

			var lastOperation = currentOperations.LastOrDefault();
			if(lastOperation != null)
				lastOperation.Invoke();
		}

		class CandidateMethod
		{
			object _Target;
			public CandidateMethod(object target)
			{
				_Target = target;
			}
			public MethodInfo Method
			{
				get;
				set;
			}
			public List<object> ParameterValues
			{
				get;
				set;
			}

			public object Invoke()
			{
				return Method.Invoke(_Target, ParameterValues.ToArray());
			}
		}

		private List<CandidateMethod> FindOperations(string name)
		{
			return
				this.GetType()
				.GetMethods()
				.Where(m => m.IsDefined(typeof(GeometryCommandAttribute)))
				.Where(m => m.GetCustomAttribute<GeometryCommandAttribute>().Command.Equals(name, StringComparison.InvariantCultureIgnoreCase))
				.Select(m => new CandidateMethod(this)
				{
					Method = m,
					ParameterValues = new List<object>()
				})
				.OrderByDescending(o => o.Method.GetParameters().Length)
				.ToList();
			;
		}
	}

	public class GeometryCommandAttribute : Attribute
	{
		public GeometryCommandAttribute(string command)
		{
			Command = command;
		}
		public string Command
		{
			get;
			set;
		}
	}
}
