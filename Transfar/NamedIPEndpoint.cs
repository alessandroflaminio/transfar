using System.Collections.Generic;
using System.Net;

namespace Transfar
{
    public class NamedIPEndPoint
    {
        public string Name { get; set; }
        public IPEndPoint EndPoint { get; set; }

        public NamedIPEndPoint(string Name, IPEndPoint EndPoint)
        {
            this.Name = Name;
            this.EndPoint = EndPoint;
        }

        public override bool Equals(object obj)
        {
            var point = obj as NamedIPEndPoint;
            return point != null &&
                   Name == point.Name &&
                   EqualityComparer<IPEndPoint>.Default.Equals(EndPoint, point.EndPoint);
        }

        public override int GetHashCode()
        {
            var hashCode = -688757198;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<IPEndPoint>.Default.GetHashCode(EndPoint);
            return hashCode;
        }

        public override string ToString()
        {
            return Name + '@' + EndPoint.ToString();
        }

        public static bool operator ==(NamedIPEndPoint point1, NamedIPEndPoint point2)
        {
            return EqualityComparer<NamedIPEndPoint>.Default.Equals(point1, point2);
        }

        public static bool operator !=(NamedIPEndPoint point1, NamedIPEndPoint point2)
        {
            return !(point1 == point2);
        }
    }
}
