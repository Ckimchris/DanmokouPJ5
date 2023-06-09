﻿using System;
using System.Runtime.CompilerServices;
using BagoumLib.Expressions;
using Danmokou.Core;
using Danmokou.Expressions;
using JetBrains.Annotations;
using UnityEngine;

namespace Danmokou.DMath {

/// <summary>
/// The combination of many <see cref="CollisionResult"/>s.
/// </summary>
public readonly struct CollisionsAccumulation {
    public readonly int damage;
    public readonly int graze;

    public CollisionsAccumulation(int dmg, int graze) {
        this.damage = dmg;
        this.graze = graze;
    }

    public static CollisionsAccumulation operator +(CollisionsAccumulation a, CollisionsAccumulation b) =>
        new(Math.Max(a.damage, b.damage), a.graze + b.graze);

    public CollisionsAccumulation WithDamage(int otherDamage) => new(Math.Max(damage, otherDamage), graze);
    public CollisionsAccumulation WithGraze(int otherGraze) => new(damage, graze + otherGraze);
}
/// <summary>
/// The result of a collision test against hard collision and graze collision.
/// </summary>
public readonly struct CollisionResult {
    public readonly bool collide;
    public readonly bool graze;

    public CollisionResult(bool collide, bool graze) {
        this.collide = collide;
        this.graze = graze;
    }

    public CollisionResult NoGraze() => new(collide, false);
}

public static class CollisionMath {
    public static readonly CollisionResult noColl = new(false, false);
    private static readonly Type t = typeof(CollisionMath);
/*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CircleOnCircle(Vector2 c1t, Vector2 c2t, float c1rc2r) {
        c2t.x -= c1t.x;
        c2t.y -= c1t.y;
        return c2t.x * c2t.x + c2t.y * c2t.y < c1rc2r * c1rc2r;
    }*/

/*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CircleOnCircle(in Hitbox c1, Vector2 c2t, float c2r) {
        c2t.x -= c1.x;
        c2t.y -= c1.y;
        c2r += c1.radius;
        return c2t.x * c2t.x + c2t.y * c2t.y < c2r * c2r;
    }*/

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CircleOnPoint(in Vector2 c1, in float r, in Vector2 c2) {
        var dx = c2.x - c1.x;
        var dy = c2.y - c1.y;
        return dx * dx + dy * dy < r * r;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CircleOnPoint(in Vector2 c1, in float r, in float c2x, in float c2y) {
        var dx = c2x - c1.x;
        var dy = c2y - c1.y;
        return dx * dx + dy * dy < r * r;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CircleOnCircle(in float c1x, in float c1y, in float c1r, in float c2x, in float c2y, in float c2r) {
        var dx = c2x - c1x;
        var dy = c2y - c1y;
        return dx * dx + dy * dy < (c1r + c2r) * (c1r + c2r);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CircleOnCircle(in Vector2 c1Loc, in float c1r, in float c2x, in float c2y, in float c2r) {
        var dx = c2x - c1Loc.x;
        var dy = c2y - c1Loc.y;
        return dx * dx + dy * dy < (c1r + c2r) * (c1r + c2r);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollisionResult GrazeCircleOnCircle(in Hurtbox h, in float x, in float y, in float r) {
        var dx = x - h.x;
        var dy = y - h.y;
        var lrSum = r + h.largeRadius;
        var rSum = r + h.radius;
        var d2 = dx * dx + dy * dy;
        return new(d2 < rSum * rSum,  d2 < lrSum * lrSum);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollisionResult GrazeCircleOnCircle(in Hurtbox h, in float x, in float y, in float r, in float scale) {
        var dx = x - h.x;
        var dy = y - h.y;
        var lrSum = (r * scale) + h.largeRadius;
        var rSum = (r * scale) + h.radius;
        var d2 = dx * dx + dy * dy;
        return new(d2 < rSum * rSum,  d2 < lrSum * lrSum);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CircleOnSegments(Vector2 target, float targetRad, Vector2 src, Vector2[] points, int start, int skip, int end, float radius, float cos_rot, float sin_rot, out int segment) {
        segment = 0;
        if (start >= end) return false;
        // use src.x to store delta vector to target, derotated.
        src.x = target.x - src.x;
        src.y = target.y - src.y;
        float _gbg = cos_rot * src.x + sin_rot * src.y;
        src.y = cos_rot * src.y - sin_rot * src.x;
        src.x = _gbg;

        float radius2 = (radius + targetRad) * (radius + targetRad);
        Vector2 delta; Vector2 g;
        float projection_unscaled; float d2;
        --end; //Now end refers to the index we will look at for the final check; ie it is inclusive.
        int ii = start + skip;
        for (; ii < end; ii += skip) {
            delta.x = points[ii].x - points[ii - skip].x;
            delta.y = points[ii].y - points[ii - skip].y;
            g.x = src.x - points[ii - skip].x;
            g.y = src.y - points[ii - skip].y;
            projection_unscaled = g.x * delta.x + g.y * delta.y;
            d2 = g.x * g.x + g.y * g.y;
            //Check circle collision at every point for accurate out segment
            if (d2 < radius2) {
                segment = ii;
                return true;
            } else if (projection_unscaled > 0) {
                float dmag2 = delta.x * delta.x + delta.y * delta.y;
                if (projection_unscaled < dmag2) {
                    float norm2 = d2 - projection_unscaled * projection_unscaled / dmag2;
                    if (norm2 < radius2) {
                        segment = ii;
                        return true;
                    }
                }
            }
        }
        //Now perform the last point check
        ii -= skip;
        segment = end;
        delta.x = points[end].x - points[ii].x;
        delta.y = points[end].y - points[ii].y;
        g.x = src.x - points[ii].x;
        g.y = src.y - points[ii].y;
        projection_unscaled = g.x * delta.x + g.y * delta.y;
        d2 = g.x * g.x + g.y * g.y;
        if (projection_unscaled < 0) {
            if (d2 < radius2) return true;
        } else {
            float dmag2 = delta.x * delta.x + delta.y * delta.y;
            if (projection_unscaled < dmag2) {
                float norm2 = d2 - projection_unscaled * projection_unscaled / dmag2;
                if (norm2 < radius2) return true;
            }
        }
        //Last point circle collision
        g.x = src.x - points[end].x;
        g.y = src.y - points[end].y;
        d2 = g.x * g.x + g.y * g.y;
        return d2 < radius2;
    }

    
    /// <summary>
    /// Check collision between a circle hurtbox and a sequence of segments with circular radii (ie. a pather or laser).
    /// </summary>
    /// <param name="c1">Circular hitbox</param>
    /// <param name="src">Base location of segments</param>
    /// <param name="points">Offset from src of each segment</param>
    /// <param name="start">First segment to consider</param>
    /// <param name="skip">Delta of segment indexes to test collision against (eg. if this is 2, then check collision on the interpolated sequence of start, start+2, start+4... end)</param>
    /// <param name="end">Last segment to consider, exclusive</param>
    /// <param name="radius">Radius of each segment point</param>
    /// <param name="cos_rot">Cosine rotation of sequence of segments</param>
    /// <param name="sin_rot">Sine rotation of sequence of segments</param>
    /// <param name="segment">Segment at which collision occurred</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollisionResult GrazeCircleOnSegments(in Hurtbox c1, Vector2 src, Vector2[] points, int start, int skip, int end, float radius, float cos_rot, float sin_rot, out int segment) {
        segment = 0;
        if (start >= end) return noColl;
        bool grazed = false;
        // use src.x to store delta vector to target, derotated.
        src.x = c1.x - src.x;
        src.y = c1.y - src.y;
        float _gbg = cos_rot * src.x + sin_rot * src.y;
        src.y = cos_rot * src.y - sin_rot * src.x;
        src.x = _gbg;

        float lradius2 = (radius + c1.largeRadius) * (radius + c1.largeRadius);
        float radius2 = (radius + c1.radius) * (radius + c1.radius);
        Vector2 delta; Vector2 g;
        float projection_unscaled; float d2;
        --end; //Now end refers to the index we will look at for the final check; ie it is inclusive.
        int ii = start + skip;
        for (; ii < end; ii += skip) {
            delta.x = points[ii].x - points[ii - skip].x;
            delta.y = points[ii].y - points[ii - skip].y;
            g.x = src.x - points[ii - skip].x;
            g.y = src.y - points[ii - skip].y;
            projection_unscaled = g.x * delta.x + g.y * delta.y;
            d2 = g.x * g.x + g.y * g.y;
            if (projection_unscaled < 0) {
                //We only check endpoint collision on the first point;
                //due to segmenting we will end by checking on all points except the last, which is handled outside.
                grazed |= d2 < lradius2;
                if (d2 < radius2) {
                    segment = ii;
                    return new CollisionResult(true, grazed);
                }
            } else {
                float dmag2 = delta.x * delta.x + delta.y * delta.y;
                if (projection_unscaled < dmag2) {
                    float norm2 = d2 - projection_unscaled * projection_unscaled / dmag2;
                    grazed |= norm2 < lradius2;
                    if (norm2 < radius2) {
                        segment = ii;
                        return new CollisionResult(true, grazed);
                    }
                }
            }
        }
        //Now perform the last point check
        ii -= skip;
        segment = end;
        delta.x = points[end].x - points[ii].x;
        delta.y = points[end].y - points[ii].y;
        g.x = src.x - points[ii].x;
        g.y = src.y - points[ii].y;
        projection_unscaled = g.x * delta.x + g.y * delta.y;
        d2 = g.x * g.x + g.y * g.y;
        if (projection_unscaled < 0) {
            grazed |= d2 < lradius2;
            if (d2 < radius2) {
                return new CollisionResult(true, grazed);
            }
        } else {
            float dmag2 = delta.x * delta.x + delta.y * delta.y;
            if (projection_unscaled < dmag2) {
                float norm2 = d2 - projection_unscaled * projection_unscaled / dmag2;
                grazed |= norm2 < lradius2;
                if (norm2 < radius2) {
                    return new CollisionResult(true, grazed);
                }
            }
        }
        //Last point circle collision
        g.x = src.x - points[end].x;
        g.y = src.y - points[end].y;
        d2 = g.x * g.x + g.y * g.y;
        grazed |= d2 < lradius2;
        return new CollisionResult(d2 < radius2, grazed);
    }


    //We use this for pill-like bullets
    //NOTE: the calling mechanism is different. You pass node1 and delta=node2-node1. The reason for this is because 
    //delta can be precomputed.
    //NOTE: it's also more efficient to compute scale stuff in here.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollisionResult GrazeCircleOnRotatedSegment(in Hurtbox h, in float x, in float y, in float radius, in Vector2 node1, in Vector2 delta, in float scale, in float delta_mag2, in float max_dist2, in Vector2 direction) {
        //First, we get src -> target and descale it, so we don't need any other scaling
        var dx = (h.x - x) / scale;
        var dy = (h.y - y) / scale;
        
        //Early exit condition: ||src -> target||^2 > 2(max_dist^2 + Lrad^2)
        //The extra 2 is because 2(x^2+y^2) is an upper bound for (x+y)^2.
        if (dx * dx + dy * dy > 2f * (max_dist2 + h.largeRadius2)) return noColl;
        
        //Derotate and subtract by node1:local to get the G vector (node1:world -> target)
        float _dx = direction.x * dx + direction.y * dy - node1.x;
        dy = direction.x * dy - direction.y * dx - node1.y;
        dx = _dx;

        float radius2 = (radius + h.radius) * (radius + h.radius);
        float lradius2 = (radius + h.largeRadius) * (radius + h.largeRadius);

        //Dot product of A:(node1:world -> target) and B:(node1 -> node2)
        float dot = dx * delta.x + dy * delta.y;
        if (dot < 0) {
            //target is in the opposite direction 
            float d2 = dx * dx + dy * dy;
            return new CollisionResult(d2 < radius2, d2 < lradius2);
        } else if (dot > delta_mag2) { //ie. proj_B(A) > ||B||
            //target is beyond node2
            dx -= delta.x;
            dy -= delta.y;
            float d2 = dx * dx + dy * dy;
            return new CollisionResult(d2 < radius2, d2 < lradius2);
        } else {
            //proj_B(A) = (dot / delta_mag)
            //We have a right triangle A, proj_B(A), norm_B(A)
            float norm = dx * dx + dy * dy - dot * dot / delta_mag2;
            return new CollisionResult(norm < radius2, norm < lradius2);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CircleOnRotatedSegment(in float cx, in float cy, in float cRad, in float x, in float y, in float radius, 
        in Vector2 node1, in Vector2 delta, in float scale, in float delta_mag2, in float max_dist2, in float cos_rot, in float sin_rot) {
        //First, we get src -> target and descale it, so we don't need any other scaling
        var dx = (cx - x) / scale;
        var dy = (cy - y) / scale;
        
        //Early exit condition: ||src -> target||^2 > 2(max_dist^2 + Lrad^2)
        //The extra 2 is because 2(x^2+y^2) is an upper bound for (x+y)^2.
        if (dx * dx + dy * dy > 2f * (max_dist2 + cRad * cRad)) return false;
        
        //Derotate and subtract by node1:local to get the G vector (node1:world -> target)
        float _dx = cos_rot * dx + sin_rot * dy - node1.x;
        dy = cos_rot * dy - sin_rot * dx - node1.y;
        dx = _dx;

        float radius2 = (radius + cRad) * (radius + cRad);

        //Dot product of A:(node1:world -> target) and B:(node1 -> node2)
        float dot = dx * delta.x + dy * delta.y;
        if (dot < 0) {
            //target is in the opposite direction 
            float d2 = dx * dx + dy * dy;
            return d2 < radius2;
        } else if (dot > delta_mag2) { //ie. proj_B(A) > ||B||
            //target is beyond node2
            dx -= delta.x;
            dy -= delta.y;
            float d2 = dx * dx + dy * dy;
            return d2 < radius2;
        } else {
            //proj_B(A) = (dot / delta_mag)
            //We have a right triangle A, proj_B(A), norm_B(A)
            float norm = dx * dx + dy * dy - dot * dot / delta_mag2;
            return norm < radius2;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UsedImplicitly]
    public static bool PointInCircle(Vector2 pt, CCircle c) {
        pt.x -= c.x;
        pt.y -= c.y;
        return pt.x * pt.x + pt.y * pt.y < c.r * c.r;
    }
    public static readonly ExFunction pointInCircle = ExFunction.Wrap(t, "PointInCircle", new[] {ExUtils.tv2, ExUtils.tcc});
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool PointInRect(Vector2 pt, CRect rect) {
        pt.x -= rect.x;
        pt.y -= rect.y;
        float px = rect.cos_rot * pt.x + rect.sin_rot * pt.y;
        pt.y = rect.cos_rot * pt.y - rect.sin_rot * pt.x;
        if (px < 0) {
            px *= -1;
        }
        if (pt.y < 0) {
            pt.y *= -1;
        }
        return px < rect.halfW && pt.y < rect.halfH;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CircleInRect(Vector2 pt, float radius, CRect rect) {
        pt.x -= rect.x;
        pt.y -= rect.y;
        float px = rect.cos_rot * pt.x + rect.sin_rot * pt.y;
        pt.y = rect.cos_rot * pt.y - rect.sin_rot * pt.x;
        if (px < 0) {
            px *= -1;
        }
        if (pt.y < 0) {
            pt.y *= -1;
        }
        return px + radius < rect.halfW && pt.y + radius < rect.halfH;
    }
    public static readonly ExFunction pointInRect = ExFunction.Wrap(t, "PointInRect", new[] {ExUtils.tv2, ExUtils.tcr});
   
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CircleOnAABB(in AABB rect, in float x, in float y, in float rad) {
        float dx = x - rect.x;
        float dy = y - rect.y;
        //Inlined absolutes are much faster
        if (dx < 0) dx *= -1;
        if (dy < 0) dy *= -1;
        dx -= rect.rx;
        dy -= rect.ry;
        return dx < rad && 
               dy < rad && 
               (dx < 0 || dy < 0 || dx * dx + dy * dy < rad * rad);
    }

    /// <summary>
    /// May report collisions when none exist, but is a good approximation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WeakCircleOnAABB(float minX, float minY, float maxX, float maxY, float dx, float dy, float r) =>
        dx > minX - r && dx < maxX + r && dy > minY - r && dy < maxY + r;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollisionResult GrazeCircleOnRect(in Hurtbox h, in float x, in float y, in Vector2 halfDim, in float diag2, in float scale, in Vector2 direction) {
        var dx = (h.x - x) / scale;
        var dy = (h.y - y) / scale;
        //Early exit condition: ||src -> target||^2 > 2*(diag^2 + Lrad^2)
        //The extra 2 is because 2(x^2+y^2) is an upper bound for (x+y)^2.
        if (dx * dx + dy * dy > 2f * (diag2 + h.largeRadius2)) return noColl;
        //First DErotate the delta vector and get its absolutes. Note we use -sin_rot
        //Store delta vector in Rect for efficiency
        float _dx = direction.x * dx + direction.y * dy;
        dy = direction.x * dy - direction.y * dx;
        dx = _dx;
        //Inlined absolutes are much faster
        if (dx < 0) dx *= -1;
        if (dy < 0) dy *= -1;
        //Then we are in one of three locations:
        if (dy < halfDim.y) {
            //In "front" of the rectangle.
            return new CollisionResult(dx - halfDim.x < h.radius,
                dx - halfDim.x < h.largeRadius);
        }
        if (dx < halfDim.x) {
            // On "top" of the rectangle
            return new CollisionResult(dy - halfDim.y < h.radius,
                dy - halfDim.y < h.largeRadius);
        }
        //In front and on top.
        dx -= halfDim.x;
        dy -= halfDim.y;
        float dsqr = dx * dx + dy * dy;
        return new CollisionResult(dsqr < h.radius2, dsqr < h.largeRadius2);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CircleOnRect(in float cx, in float cy, in float cRad, in float x, in float y, in float rectHalfX, in float rectHalfY, 
        in float diag2, in float scale, in float cos_rot, in float sin_rot) {
        var dx = (cx - x) / scale;
        var dy = (cy - y) / scale;
        //Early exit condition: ||src -> target||^2 > 2*(diag^2 + Lrad^2)
        //The extra 2 is because 2(x^2+y^2) is an upper bound for (x+y)^2.
        if (dx * dx + dy * dy > 2f * (diag2 + cRad * cRad)) return false;
        //First DErotate the delta vector and get its absolutes. Note we use -sin_rot
        //Store delta vector in Rect for efficiency
        float _dx = cos_rot * dx + sin_rot * dy;
        dy = cos_rot * dy - sin_rot * dx;
        dx = _dx;
        //Inlined absolutes are much faster
        if (dx < 0) dx *= -1;
        if (dy < 0) dy *= -1;
        //Then we are in one of three locations:
        if (dy < rectHalfY) {
            //In "front" of the rectangle.
            return dx - rectHalfX < cRad;
        }
        if (dx < rectHalfX) {
            // On "top" of the rectangle
            return dy - rectHalfY < cRad;
        }
        //In front and on top.
        dx -= rectHalfX;
        dy -= rectHalfY;
        return dx * dx + dy * dy < cRad * cRad;
    }
}
}
