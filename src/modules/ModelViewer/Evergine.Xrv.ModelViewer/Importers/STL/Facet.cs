// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Mathematics;

namespace Evergine.Xrv.ModelViewer.Importers.STL
{
    internal struct Facet
    {
        public Vector3 normal;

        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Facet(Vector3 normal, Vector3 a, Vector3 b, Vector3 c)
        {
            this.normal = normal;
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public override string ToString()
        {
            return string.Format("{0:F2}: {1:F2}, {2:F2}, {3:F2}", this.normal, this.a, this.b, this.c);
        }
    }
}
