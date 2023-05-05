﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Ray = MathUtils.Ray;
using Vector3 = Godot.Vector3;

public class Voronoi
{
    public Array<Triangle>? Mesh;
    private readonly List<Tetrahedron> delaunayDiagram;

    // very big tetrahedron, will automate making this
    private readonly float[][] bigTetra =
    {
        new[] { 0, -24999.75f, -74999.25f },
        new[] { 0, 74999.25f, 0 },
        new[] { 53032.61968f, -24999.75f, -53032.61968f },
        new[] { -53032.61968f, -24999.75f, -53032.61968f },
    };

    public Voronoi(List<Vector3> seeds)
    {
        // big tetrahedron that contains all points to start
        var bigTetVerts = new[]
        {
            new Vector3(bigTetra[0][0], bigTetra[0][1], bigTetra[0][2]),
            new Vector3(bigTetra[1][0], bigTetra[1][1], bigTetra[1][2]),
            new Vector3(bigTetra[2][0], bigTetra[2][1], bigTetra[2][2]),
            new Vector3(bigTetra[3][0], bigTetra[3][1], bigTetra[3][2]),
        };

        Tetrahedron bigTet = new(bigTetVerts, 0);
        delaunayDiagram = new List<Tetrahedron> { bigTet };

        // Stage 1, make a basic diagram
        InitializeDiagram(seeds);

        // Stage 2, make a final diagram
        // DelIso(delaunayDiagram);

        // Final Stage, make a mesh for Godot to use
        // ToMesh(delaunayDiagram);
    }

    /// <summary>
    ///   determines if a point p is over, under or lies on a plane defined by three points a, b and c
    /// </summary>
    /// <returns>
    ///   a positive value when the point p is above the plane defined by a, b and c; a negative value
    ///   if p is under the plane; and exactly 0 if p is directly on the plane.
    /// </returns>
    private static float Orient(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
    {
        float[,] orientMatrix =
        {
            { a.x, a.y, a.z, 1.0f },
            { b.x, b.y, b.z, 1.0f },
            { c.x, c.y, c.z, 1.0f },
            { p.x, p.y, p.z, 1.0f },
        };

        float determinant =
            orientMatrix[0, 0] * (orientMatrix[1, 1] * orientMatrix[2, 2] - orientMatrix[1, 2] * orientMatrix[2, 1]) -
            orientMatrix[0, 1] * (orientMatrix[1, 0] * orientMatrix[2, 2] - orientMatrix[1, 2] * orientMatrix[2, 0]) +
            orientMatrix[0, 2] * (orientMatrix[1, 0] * orientMatrix[2, 1] - orientMatrix[1, 1] * orientMatrix[2, 0]);

        return determinant;
    }

    /// <summary>
    ///   finds the center of the sphere that passes through all 4 vertices of a tetrahedron
    /// </summary>
    /// <returns>
    ///   circumcenter of a tetrahedron
    /// </returns>
    private static Vector3 FindCircumcenter(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Vector3 diff1 = b - a;
        float sqLen1 = Mathf.Pow(diff1.Length(), 2);

        Vector3 diff2 = c - b;
        float sqLen2 = Mathf.Pow(diff2.Length(), 2);

        Vector3 diff3 = d - c;
        float sqLen3 = Mathf.Pow(diff3.Length(), 2);

        float[,] circMatrix =
        {
            { diff1.x, diff1.y, diff1.z },
            { diff2.x, diff2.y, diff2.z },
            { diff3.x, diff3.y, diff3.z },
        };

        float determinant =
            circMatrix[0, 0] * (circMatrix[1, 1] * circMatrix[2, 2] - circMatrix[1, 2] * circMatrix[2, 1]) -
            circMatrix[0, 1] * (circMatrix[1, 0] * circMatrix[2, 2] - circMatrix[1, 2] * circMatrix[2, 0]) +
            circMatrix[0, 2] * (circMatrix[1, 0] * circMatrix[2, 1] - circMatrix[1, 1] * circMatrix[2, 0]);

        float volume = determinant / 6.0f;
        float i12Volume = 1.0f / (volume * 12.0f);

        Vector3 center = new(
            a.x + i12Volume * (
                (circMatrix[1, 1] * circMatrix[2, 2] - circMatrix[2, 1] * circMatrix[1, 2]) * sqLen1
                - (circMatrix[0, 1] * circMatrix[2, 2] - circMatrix[2, 1] * circMatrix[0, 2]) * sqLen2
                + (circMatrix[0, 1] * circMatrix[1, 2] - circMatrix[1, 1] * circMatrix[0, 2]) * sqLen3),
            a.y + i12Volume * (
                -(circMatrix[1, 0] * circMatrix[2, 2] - circMatrix[2, 0] * circMatrix[1, 2]) * sqLen1
                + (circMatrix[0, 0] * circMatrix[2, 2] - circMatrix[2, 0] * circMatrix[0, 2]) * sqLen2
                - (circMatrix[0, 0] * circMatrix[1, 2] - circMatrix[1, 0] * circMatrix[0, 2]) * sqLen3),
            a.z + i12Volume * (
                (circMatrix[1, 0] * circMatrix[2, 1] - circMatrix[2, 0] * circMatrix[1, 1]) * sqLen1
                - (circMatrix[0, 0] * circMatrix[1, 1] - circMatrix[2, 0] * circMatrix[0, 1]) * sqLen2
                + (circMatrix[0, 0] * circMatrix[1, 1] - circMatrix[1, 0] * circMatrix[0, 1]) * sqLen3));
        return center;
    }

    /// <summary>
    ///   determines if a point p is inside, outside or lies on a sphere defined by four points a, b, c and d.
    /// </summary>
    /// <returns>
    ///   a positive value is returned if p is inside the sphere; a negative if p is outside; and exactly 0 if p
    ///   is directly on the sphere.
    /// </returns>
    private static float InSphere(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 p)
    {
        float[,] inSphereMatrix =
        {
            { a.x, a.y, a.z, (a.x * a.x) + (a.y * a.y) + (a.z * a.z), 1.0f },
            { b.x, b.y, b.z, (b.x * b.x) + (b.y * b.y) * (b.z * b.z), 1.0f },
            { c.x, c.y, c.z, (c.x * c.x) + (c.y * c.y) * (c.z * c.z), 1.0f },
            { d.x, d.y, d.z, (d.x * d.x) + (d.y * d.y) * (d.z * d.z), 1.0f },
            { p.x, p.y, p.z, (p.x * p.x) + (p.y * p.y) * (p.z * p.z), 1.0f },
        };

        float determinant =
            inSphereMatrix[0, 3] * inSphereMatrix[1, 2] * inSphereMatrix[2, 1] * inSphereMatrix[3, 0] -
            inSphereMatrix[0, 2] * inSphereMatrix[1, 3] * inSphereMatrix[2, 1] * inSphereMatrix[3, 0] -
            inSphereMatrix[0, 3] * inSphereMatrix[1, 1] * inSphereMatrix[2, 2] * inSphereMatrix[3, 0] +
            inSphereMatrix[0, 1] * inSphereMatrix[1, 3] * inSphereMatrix[2, 2] * inSphereMatrix[3, 0] +
            inSphereMatrix[0, 2] * inSphereMatrix[1, 1] * inSphereMatrix[2, 3] * inSphereMatrix[3, 0] -
            inSphereMatrix[0, 1] * inSphereMatrix[1, 2] * inSphereMatrix[2, 3] * inSphereMatrix[3, 0] -
            inSphereMatrix[0, 3] * inSphereMatrix[1, 2] * inSphereMatrix[2, 0] * inSphereMatrix[3, 1] +
            inSphereMatrix[0, 2] * inSphereMatrix[1, 3] * inSphereMatrix[2, 0] * inSphereMatrix[3, 1] +
            inSphereMatrix[0, 3] * inSphereMatrix[1, 0] * inSphereMatrix[2, 2] * inSphereMatrix[3, 1] -
            inSphereMatrix[0, 0] * inSphereMatrix[1, 3] * inSphereMatrix[2, 2] * inSphereMatrix[3, 1] -
            inSphereMatrix[0, 2] * inSphereMatrix[1, 0] * inSphereMatrix[2, 3] * inSphereMatrix[3, 1] +
            inSphereMatrix[0, 0] * inSphereMatrix[1, 2] * inSphereMatrix[2, 3] * inSphereMatrix[3, 1] +
            inSphereMatrix[0, 3] * inSphereMatrix[1, 1] * inSphereMatrix[2, 0] * inSphereMatrix[3, 2] -
            inSphereMatrix[0, 1] * inSphereMatrix[1, 3] * inSphereMatrix[2, 0] * inSphereMatrix[3, 2] -
            inSphereMatrix[0, 3] * inSphereMatrix[1, 0] * inSphereMatrix[2, 1] * inSphereMatrix[3, 2] +
            inSphereMatrix[0, 0] * inSphereMatrix[1, 3] * inSphereMatrix[2, 1] * inSphereMatrix[3, 2] +
            inSphereMatrix[0, 1] * inSphereMatrix[1, 0] * inSphereMatrix[2, 3] * inSphereMatrix[3, 2] -
            inSphereMatrix[0, 0] * inSphereMatrix[1, 1] * inSphereMatrix[2, 3] * inSphereMatrix[3, 2] -
            inSphereMatrix[0, 2] * inSphereMatrix[1, 1] * inSphereMatrix[2, 0] * inSphereMatrix[3, 3] +
            inSphereMatrix[0, 1] * inSphereMatrix[1, 2] * inSphereMatrix[2, 0] * inSphereMatrix[3, 3] +
            inSphereMatrix[0, 2] * inSphereMatrix[1, 0] * inSphereMatrix[2, 1] * inSphereMatrix[3, 3] -
            inSphereMatrix[0, 0] * inSphereMatrix[1, 2] * inSphereMatrix[2, 1] * inSphereMatrix[3, 3] -
            inSphereMatrix[0, 1] * inSphereMatrix[1, 0] * inSphereMatrix[2, 2] * inSphereMatrix[3, 3] +
            inSphereMatrix[0, 0] * inSphereMatrix[1, 1] * inSphereMatrix[2, 2] * inSphereMatrix[3, 3];

        return determinant;
    }

    /// <summary>
    ///   <para>
    ///     Hugo Ledoux 'Computing the 3D Voronoi Diagram Robustly: An Easy Explanation'
    ///   </para>
    ///   Delft University of Technology (OTB-section GIS Technology) [Internet] 2007
    ///   <para>
    ///     Available from: http://www.gdmc.nl/publications/2007/Computing_3D_Voronoi_Diagram.pdf
    ///   </para>
    /// </summary>
    private void InitializeDiagram(List<Vector3> seeds)
    {
        // insert seeds as query points, then rebuild diagram until we triangulate every point.
        // gives low poly initial triangulation that we make more detailed with Del-Iso
        int listPos = 0;
        for (int i = 0; i < seeds.Count; i++)
        {
            Vector3 query = seeds[i];
            InsertPoint(delaunayDiagram, query, ref listPos);
        }

        Mesh = new Array<Triangle>();

        // testing triangulation
        for (int i = 0; i < delaunayDiagram.Count; i++)
        {
            for (int k = 0; k < 4; k++)
            {
                Mesh.Add(delaunayDiagram[i].Faces[k]);
            }
        }
    }

    // this inserts a point and calculates new tetrahedra
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InsertPoint(List<Tetrahedron> delaunay, Vector3 point, ref int listPos)
    {
        Tetrahedron tetra;
        if (delaunay[listPos].HasNeighbors)
        {
            tetra = Walk(delaunay[listPos], point);
        }
        else
        {
            tetra = delaunay[listPos];
        }

        listPos = delaunayDiagram.IndexOf(tetra);
        delaunayDiagram.Remove(tetra);

        // insert point in tetra with a flip14
        Tetrahedron a = new(new[] { point, tetra.Vertices[0], tetra.Vertices[1], tetra.Vertices[2] },
            listPos);
        Tetrahedron b = new(new[] { point, tetra.Vertices[0], tetra.Vertices[2], tetra.Vertices[3] },
            listPos++);
        Tetrahedron c = new(new[] { point, tetra.Vertices[0], tetra.Vertices[3], tetra.Vertices[1] },
            listPos + 2);
        Tetrahedron d = new(new[] { point, tetra.Vertices[1], tetra.Vertices[2], tetra.Vertices[3] },
            listPos + 3);

        // TODO: gotta figure out way to map adjacency to bigger tetras outside this one
        a.Neighbors[0] = b.ListPos;
        a.Neighbors[1] = c.ListPos;
        a.Neighbors[2] = d.ListPos;

        b.Neighbors[0] = c.ListPos;
        b.Neighbors[1] = d.ListPos;
        b.Neighbors[2] = b.ListPos;

        c.Neighbors[0] = d.ListPos;
        c.Neighbors[1] = b.ListPos;
        c.Neighbors[2] = c.ListPos;

        delaunayDiagram.Add(a);
        delaunayDiagram.Add(b);
        delaunayDiagram.Add(c);
        delaunayDiagram.Add(d);

        listPos += 3;

        Stack<Tetrahedron> newTetras = new();
        newTetras.Push(a);
        newTetras.Push(b);
        newTetras.Push(c);
        newTetras.Push(d);

        while (newTetras.Count > 0)
        {
            // tetra = {p, a, b, c} <--pop from stack
            var test = newTetras.Pop();
            listPos = test.ListPos;

            if (test.Neighbors[3] != -1)
            {
                // tetra[a] = {a, b, c, d} <--get adjacent tetra of delaunay having abc as a face
                var neighbor = delaunayDiagram[test.Neighbors[3]];

                // if d is inside circumsphere of tetra then flip
                if (InSphere(test.Vertices[0], test.Vertices[1], test.Vertices[2], test.Vertices[3],
                        neighbor.Vertices[0]) > 0)
                {
                    Flip(test, neighbor, ref newTetras, ref listPos);
                }
            }
        }
    }

    /// <summary>
    ///   <para>
    ///     O.Devillers, S.Pion, and M.Teillaud 'Walking in a Triangulation'
    ///   </para>
    ///   International Journal of Foundations of Computer Science, 13(2):181-199, 2002
    ///   <para>
    ///     Available from: https://inria.hal.science/inria-00102194/document
    ///   </para>
    /// </summary>
    /// <param name="tetra">tetrahedron to walk from</param>
    /// <param name="point">query point</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Tetrahedron Walk(Tetrahedron tetra, Vector3 point)
    {
        // Remembering Stochastic Walk
        // from some starting vertex to point. tetra=qrlt is a tetrahedron of simplexes (faces)
        // previous = tetra; end = false;
        var previous = tetra;
        bool end = false;

        while (!end)
        {
            // face = random facet of tetra;
            int random = new Random().Next(3);
            Triangle face = tetra.Faces[random];

            // TODO: need to map neighbors to edges
            Tetrahedron neighbor = delaunayDiagram[tetra.Neighbors[random]];

            // don't go backwards
            bool neighborPrevious = neighbor == previous;

            // point on other side of edge
            bool otherSide = Orient(face.Vertices[0], face.Vertices[1], face.Vertices[2], point) > 0;

            // if( point not neighbor of previous through face ) && ( point on other side of face )
            if (!neighborPrevious && otherSide)
            {
                previous = tetra;
                tetra = neighbor;
            }
            else
            {
                // face = next facet of tetra;
                face = random < 3 ? tetra.Faces[random++] : tetra.Faces[0];

                neighbor = delaunayDiagram[tetra.Neighbors[random]];
                neighborPrevious = neighbor == previous;
                otherSide = Orient(face.Vertices[0], face.Vertices[1], face.Vertices[2], point) > 0;

                if (!neighborPrevious && otherSide)
                {
                    previous = tetra;
                    tetra = neighbor;
                }
                else
                {
                    // face = next facet of tetra;
                    face = random < 3 ? tetra.Faces[random++] : tetra.Faces[0];

                    neighbor = delaunayDiagram[tetra.Neighbors[random]];
                    neighborPrevious = neighbor == previous;
                    otherSide = Orient(face.Vertices[0], face.Vertices[1], face.Vertices[2], point) > 0;

                    if (!neighborPrevious && otherSide)
                    {
                        previous = tetra;
                        tetra = neighbor;
                    }
                    else
                    {
                        end = true;
                    }
                }
            }
        }

        // tetra contains point
        return tetra;
    }

    // A technique where we split up a tetrahedron
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Flip(Tetrahedron tetraA, Tetrahedron tetraB, ref Stack<Tetrahedron> newTetras, ref int listPos)
    {
        Ray intersectCheck = new(tetraA.Vertices[0], tetraB.Vertices[0] - tetraA.Vertices[0]);

        // case #1: convex
        if (MathUtils.Intersect(tetraA.Vertices[1], tetraA.Vertices[2], tetraA.Vertices[3], intersectCheck))
        {
            // flip23(A, B)
            delaunayDiagram.Remove(tetraA);
            delaunayDiagram.Remove(tetraB);

            Tetrahedron pabd = new(new[]
            {
                tetraA.Vertices[0], tetraA.Vertices[1], tetraA.Vertices[2], tetraB.Vertices[0],
            }, listPos--);

            Tetrahedron pbcd = new(new[]
            {
                tetraA.Vertices[0], tetraA.Vertices[2], tetraA.Vertices[3], tetraB.Vertices[0],
            }, listPos);

            Tetrahedron pcad = new(new[]
            {
                tetraA.Vertices[0], tetraA.Vertices[3], tetraA.Vertices[1], tetraB.Vertices[0],
            }, listPos++);

            delaunayDiagram.Add(pabd);
            delaunayDiagram.Add(pbcd);
            delaunayDiagram.Add(pcad);

            pabd.Neighbors[0] = pbcd.ListPos;
            pabd.Neighbors[1] = pcad.ListPos;

            pbcd.Neighbors[0] = pcad.ListPos;
            pbcd.Neighbors[1] = pabd.ListPos;

            pcad.Neighbors[0] = pabd.ListPos;
            pcad.Neighbors[1] = pbcd.ListPos;

            listPos++;

            // push tetra pabd, pbcd, and pacd on stack
            newTetras.Push(pabd);
            newTetras.Push(pbcd);
            newTetras.Push(pcad);
            return;
        }

        float epsilon = MathUtils.EPSILON;

        for (int i = 0; i < 3; i++)
        {
            Triangle testTri = tetraA.Faces[i];
            Tetrahedron pdab = new(new[]
            {
                tetraA.Vertices[0], tetraB.Vertices[0], testTri.Vertices[2], testTri.Vertices[1],
            }, tetraA.Neighbors[i]);
            Tetrahedron neighborAtFace = delaunayDiagram[tetraA.Neighbors[i]];

            // case #2: concave && diagram(?) has tetra pdab
            if (neighborAtFace.Vertices == pdab.Vertices)
            {
                // flip32(A, B, pdab)
                delaunayDiagram.Remove(tetraA);
                delaunayDiagram.Remove(tetraB);

                Tetrahedron pacd = new(new[]
                {
                    tetraA.Vertices[0], tetraA.Vertices[1], tetraA.Vertices[3], tetraB.Vertices[0],
                }, listPos--);

                delaunayDiagram.Add(pacd);
                pacd.Neighbors[0] = pdab.ListPos;
                listPos--;

                // push pacd and pdab on stack
                newTetras.Push(pacd);
                newTetras.Push(pdab);
                return;
            }

            float planarTest = Orient(testTri.Vertices[0], testTri.Vertices[1], testTri.Vertices[2],
                tetraB.Vertices[0]);

            bool coplanar = planarTest <= epsilon && planarTest >= -epsilon;
            bool config44 = tetraA.Neighbors[i] != -1 && tetraB.Neighbors[i] != -1
                && tetraA.Neighbors[i] != tetraB.Neighbors[i];

            // case #3: degenerate coplanar && A and B are in config44 w/ C and D
            if (coplanar && config44)
            {
                Tetrahedron neighborA = delaunayDiagram[tetraA.Neighbors[i]];
                Tetrahedron neighborB = delaunayDiagram[tetraB.Neighbors[i]];

                delaunayDiagram.Remove(tetraA);
                delaunayDiagram.Remove(tetraB);
                delaunayDiagram.Remove(neighborA);
                delaunayDiagram.Remove(neighborB);

                // flip44(A, B, C, D)
                Tetrahedron flipA = new(new[]
                {
                    testTri.Vertices[0], neighborA.Vertices[3], tetraA.Vertices[Mathf.Abs(i - 3)], testTri.Vertices[1],
                }, listPos - 3);

                Tetrahedron flipB = new(new[]
                {
                    tetraB.Vertices[0], tetraA.Vertices[Mathf.Abs(i - 3)], neighborB.Vertices[2], testTri.Vertices[1],
                }, listPos - 2);

                Tetrahedron flipC = new(new[]
                {
                    tetraA.Vertices[0], tetraA.Vertices[Mathf.Abs(i - 3)], neighborA.Vertices[3], testTri.Vertices[2],
                }, listPos--);

                Tetrahedron flipD = new(new[]
                {
                    tetraB.Vertices[0], neighborB.Vertices[2], tetraA.Vertices[Mathf.Abs(i - 3)], testTri.Vertices[2],
                }, listPos);

                delaunayDiagram.Add(flipA);
                delaunayDiagram.Add(flipB);
                delaunayDiagram.Add(flipC);
                delaunayDiagram.Add(flipD);

                flipA.Neighbors[0] = flipC.ListPos;
                flipA.Neighbors[1] = tetraA.Neighbors[Mathf.Abs(i - 2)];
                flipA.Neighbors[2] = neighborA.Neighbors[1];
                flipA.Neighbors[3] = flipB.ListPos;

                flipB.Neighbors[0] = flipD.ListPos;
                flipB.Neighbors[1] = tetraB.Neighbors[1];
                flipB.Neighbors[2] = neighborB.Neighbors[Mathf.Abs(i - 2)];
                flipB.Neighbors[3] = flipA.ListPos;

                flipC.Neighbors[0] = neighborA.Neighbors[i];
                flipC.Neighbors[1] = tetraA.Neighbors[1];
                flipC.Neighbors[2] = flipA.ListPos;
                flipC.Neighbors[3] = flipD.ListPos;

                flipD.Neighbors[0] = flipB.ListPos;
                flipD.Neighbors[1] = neighborB.Neighbors[i - 2];
                flipD.Neighbors[2] = tetraB.Neighbors[i - 2];
                flipD.Neighbors[3] = flipC.ListPos;

                // push on stack the 4 tetra created
                newTetras.Push(flipA);
                newTetras.Push(flipB);
                newTetras.Push(flipC);
                newTetras.Push(flipD);

                return;
            }
        }

        // case #4: degenerate flat tetra
        for (int i = 0; i < 4; i++)
        {
            float planarTest = Orient(tetraA.Vertices[1], tetraA.Vertices[2], tetraA.Vertices[3],
                tetraA.Vertices[0]);
            if (planarTest <= epsilon && planarTest >= -epsilon)
            {
                // flip23(A, B)
                delaunayDiagram.Remove(tetraA);
                delaunayDiagram.Remove(tetraB);

                Tetrahedron pabd = new(new[]
                {
                    tetraA.Vertices[0], tetraA.Vertices[1], tetraA.Vertices[2], tetraB.Vertices[0],
                }, listPos++);
                listPos++;

                Tetrahedron pbcd = new(new[]
                {
                    tetraA.Vertices[0], tetraA.Vertices[2], tetraA.Vertices[3], tetraB.Vertices[0],
                }, listPos++);
                listPos++;

                Tetrahedron pacd = new(new[]
                {
                    tetraA.Vertices[0], tetraA.Vertices[3], tetraA.Vertices[2], tetraB.Vertices[0],
                }, listPos++);
                listPos++;

                delaunayDiagram.Add(pabd);
                delaunayDiagram.Add(pbcd);
                delaunayDiagram.Add(pacd);

                listPos = pacd.ListPos;

                // push tetra pabd, pbcd, and pacd on stack
                newTetras.Push(pabd);
                newTetras.Push(pbcd);
                newTetras.Push(pacd);
            }
        }
    }

    /// <summary>
    ///   <para>
    ///     T.K.Dey, J.A.Levine 'Delaunay Meshing of Isosurfaces'
    ///   </para>
    ///   Proc.Shape Modeling International, 2007
    ///   <para>
    ///     Available from: http://web.cse.ohio-state.edu/~dey.8/deliso.html
    ///   </para>
    /// </summary>
    private void DelIso(List<Tetrahedron> delaunay)
    {
        RecoverDiagram(delaunay);

        // RefineDiagram(voronoi);

        throw new NotImplementedException();
    }

    private ICollection<Cell> RecoverDiagram(List<Tetrahedron> delaunay)
    {
        throw new NotImplementedException();
    }

    private ICollection<Cell> RefineDiagram(ICollection<Cell> diagram)
    {
        throw new NotImplementedException();
    }

    // voronoi cell
    public struct Cell
    {
        public List<Vector3> Vertices;
        public Vector3 Site;

        public Cell(Vector3 seed, List<Vector3> verts)
        {
            Site = seed;
            Vertices = verts;
        }
    }

    // delaunay triangle
    public struct Triangle
    {
        public Vector3[] Vertices;

        public Triangle(float[] a, float[] b, float[] c)
        {
            Vertices = new[]
            {
                new Vector3(a[0], a[1], a[2]),
                new Vector3(b[0], b[1], b[2]),
                new Vector3(c[0], c[1], c[2]),
            };
        }
    }

    // a tetrahedron that helps form a 3D delaunay diagram
    public struct Tetrahedron : IEquatable<Tetrahedron>
    {
        public int ListPos;
        public Vector3[] Vertices;
        public Triangle[] Faces;
        public int[] Neighbors;
        public bool HasNeighbors;
        public Vector3 Circumcenter;

        public Tetrahedron(Vector3[] verts, int listPos)
        {
            ListPos = listPos;

            Vertices = verts;

            float[] q = { verts[0].x, verts[0].y, verts[0].z };
            float[] r = { verts[1].x, verts[1].y, verts[1].z };
            float[] l = { verts[2].x, verts[2].y, verts[2].z };
            float[] t = { verts[3].x, verts[3].y, verts[3].z };

            Faces = new[]
            {
                new Triangle(q, r, l),
                new Triangle(q, t, r),
                new Triangle(q, l, t),
                new Triangle(r, t, l),
            };

            HasNeighbors = false;

            // edge flags to ID neighbors; each int is the index of the neighbor
            Neighbors = new[]
            {
                -1, -1, -1, -1,
            };

            Circumcenter = FindCircumcenter(Vertices[0], Vertices[1], Vertices[2], Vertices[3]);
        }

        public static bool operator ==(Tetrahedron tetra, Tetrahedron other)
        {
            return tetra.Equals(other);
        }

        public static bool operator !=(Tetrahedron tetra, Tetrahedron other)
        {
            return !tetra.Equals(other);
        }

        public bool Equals(Tetrahedron other)
        {
            return Vertices == other.Vertices;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Tetrahedron tetra))
                return false;

            return Equals(tetra);
        }

        public override int GetHashCode()
        {
            var hashCode = -1949845991;

            hashCode = hashCode * -1287342897 + Vertices.GetHashCode();
            hashCode = hashCode * -1287342897 + Faces.GetHashCode();
            hashCode = hashCode * -1287342897 + Neighbors.GetHashCode();

            return hashCode;
        }
    }
}
