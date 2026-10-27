using System.Collections.Generic;
using UnityEngine;

namespace GEL
{
    public enum DribbleStyle
    {
        Simple,
        Feint,
        Roulette,
        DoubleTouch,
        StepOver,
        Elastico
    }

    public static class DribbleGestureRecognizer
    {
        public static DribbleStyle Recognize(
            List<Vector2> points,
            out float confidence,
            out float feintAngle,
            out float cumulativeTurn)
        {
            confidence = 0f;
            feintAngle = 0f;
            cumulativeTurn = 0f;

            if (points == null || points.Count < 4)
                return DribbleStyle.Simple;

            Vector2 start = points[0];
            Vector2 end = points[points.Count - 1];
            Vector2 net = end - start;
            float pathLength = 0f;

            for (int i = 1; i < points.Count; i++)
                pathLength += Vector2.Distance(points[i - 1], points[i]);

            if (pathLength < 1f || net.magnitude < 1f)
                return DribbleStyle.Simple;

            float earlyTarget = pathLength * 0.32f;
            float exitTarget = pathLength * 0.65f;
            float accumulated = 0f;
            Vector2 earlyEnd = start;
            Vector2 exitStart = start;

            for (int i = 1; i < points.Count; i++)
            {
                accumulated += Vector2.Distance(points[i - 1], points[i]);
                if (accumulated >= earlyTarget && earlyEnd == start)
                    earlyEnd = points[i];
                if (accumulated >= exitTarget)
                {
                    exitStart = points[i];
                    break;
                }
            }

            Vector2 earlyDirection = earlyEnd - start;
            Vector2 exitDirection = end - exitStart;
            if (earlyDirection.sqrMagnitude > 0.01f && exitDirection.sqrMagnitude > 0.01f)
                feintAngle = Vector2.Angle(earlyDirection, exitDirection);

            int reversals = 0;
            float lastSign = 0f;

            for (int i = 2; i < points.Count; i++)
            {
                Vector2 previous = points[i - 1] - points[i - 2];
                Vector2 current = points[i] - points[i - 1];
                if (previous.sqrMagnitude < 1f || current.sqrMagnitude < 1f) continue;

                cumulativeTurn += Vector2.SignedAngle(previous, current);
                if (Vector2.Dot(previous.normalized, current.normalized) < -0.2f)
                {
                    float sign = Mathf.Sign(Vector2.Dot(
                        new Vector2(-previous.y, previous.x),
                        current));
                    if (lastSign != 0f && sign != lastSign)
                        reversals++;
                    lastSign = sign;
                }
            }

            float area = Mathf.Abs(SignedArea(points)) /
                         Mathf.Max(1f, pathLength * pathLength);

            // Elastico: a low-to-high chevron (a reversed > or <). The path
            // rises vertically, has a sharp apex, and is longer than its net.
            if (Mathf.Abs(net.y) / Mathf.Max(1f, pathLength) > 0.10f &&
                pathLength > net.magnitude * 1.3f)
            {
                int apexIndex = 0;
                float bestDeviation = 0f;
                Vector2 netDirection = net.normalized;

                for (int i = 1; i < points.Count - 1; i++)
                {
                    Vector2 relative = points[i] - start;
                    Vector2 perpendicular = relative -
                        netDirection * Vector2.Dot(relative, netDirection);
                    if (perpendicular.magnitude > bestDeviation)
                    {
                        bestDeviation = perpendicular.magnitude;
                        apexIndex = i;
                    }
                }

                if (apexIndex > 0)
                {
                    Vector2 first = points[apexIndex] - start;
                    Vector2 second = end - points[apexIndex];
                    if (first.sqrMagnitude > 1f && second.sqrMagnitude > 1f)
                    {
                        float apexAngle = Vector2.Angle(first, second);
                        if (apexAngle > 48f && bestDeviation > pathLength * 0.06f)
                        {
                            confidence = 0.86f;
                            return DribbleStyle.Elastico;
                        }
                    }
                }
            }

            if (reversals >= 2 && feintAngle > 55f)
            {
                confidence = 0.82f;
                return DribbleStyle.DoubleTouch;
            }

            if (Mathf.Abs(cumulativeTurn) > 180f && pathLength > net.magnitude * 1.35f)
            {
                confidence = 0.78f;
                return DribbleStyle.Roulette;
            }

            if (feintAngle > 70f && pathLength > net.magnitude * 1.05f)
            {
                confidence = 0.76f;
                return DribbleStyle.Feint;
            }

            if (area > 0.055f && pathLength > net.magnitude * 1.4f)
            {
                confidence = 0.64f;
                return DribbleStyle.StepOver;
            }

            confidence = 0.55f;
            return DribbleStyle.Simple;
        }

        private static float SignedArea(List<Vector2> points)
        {
            float area = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Count];
                area += a.x * b.y - b.x * a.y;
            }
            return area * 0.5f;
        }
    }
}
