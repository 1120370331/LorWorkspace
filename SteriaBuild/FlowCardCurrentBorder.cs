using UnityEngine;
using UnityEngine.UI;

namespace Steria
{
    // One reusable mesh follows the native frame bounds in card-local space.
    // Its owner stamp drives the clock so disabling a pooled card cannot leave a live decoration.
    public sealed class FlowCardCurrentBorder : MaskableGraphic
    {
        private BattleDiceCardUI _owner;
        private BattleDiceCardModel _card;
        private int _levels;
        private float _clock;
        private readonly Vector3[] _corners = new Vector3[4];
        private const float LapSeconds = 2.4f;

        public static void Apply(BattleDiceCardUI ui, FlowCardPreviewStamp stamp)
        {
            Image frame = null;
            if (ui.img_linearDodges != null)
                foreach (var image in ui.img_linearDodges)
                    if (image != null && (frame == null ||
                        image.rectTransform.rect.width * image.rectTransform.rect.height >
                        frame.rectTransform.rect.width * frame.rectTransform.rect.height)) frame = image;
            if (frame == null) { if (stamp.CurrentBorder != null) stamp.CurrentBorder.Hide(); return; }
            var border = stamp.CurrentBorder;
            if (border == null)
            {
                var go = new GameObject("SteriaFlowCurrentBorder", typeof(RectTransform), typeof(FlowCardCurrentBorder));
                border = go.GetComponent<FlowCardCurrentBorder>();
                stamp.CurrentBorder = border;
                border.raycastTarget = false;
            }
            if (border.transform.parent != ui.transform) border.transform.SetParent(ui.transform, false);
            var rect = border.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one;
            border._owner = ui; border._card = ui.CardModel;
            border.Tick(Time.unscaledTime);
        }

        public void Hide()
        {
            _levels = 0;
            enabled = false;
            canvasRenderer.Clear();
        }

        public void Tick(float now)
        {
            var plan = _owner != null && _owner.gameObject.activeInHierarchy &&
                ReferenceEquals(_owner.CardModel, _card) ? FlowPlanning.Get(_card) : null;
            if (plan == null || plan.Levels <= 0) { Hide(); return; }
            _levels = plan.Levels;
            _clock = now;
            if (!SyncNativeFrame()) { Hide(); return; }
            enabled = true;
            SetVerticesDirty();
        }

        private bool SyncNativeFrame()
        {
            if (_owner.img_linearDodges == null) return false;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var image in _owner.img_linearDodges)
            {
                if (image == null || image.rectTransform.rect.width <= 0 || image.rectTransform.rect.height <= 0) continue;
                image.rectTransform.GetWorldCorners(_corners);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 point = _owner.transform.InverseTransformPoint(_corners[i]);
                    min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                }
            }
            if (float.IsInfinity(min.x)) return false;
            rectTransform.localPosition = (min + max) * .5f;
            rectTransform.sizeDelta = max - min;
            return true;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_levels <= 0) return;
            var rect = rectTransform.rect;
            if (rect.width <= 0 || rect.height <= 0) return;
            float scale = Mathf.Min(rect.width, rect.height) / 208f;
            float strength = 1 + Mathf.Min(_levels - 1, 3) * .055f;
            float phase = Mathf.Repeat(_clock / LapSeconds, 1);
            // Two short separated currents; almost all of the native frame remains negative space.
            Stream(vh, rect, phase, .025f, scale, strength);
            Stream(vh, rect, phase + .53f, .022f, scale, strength * .82f);
            for (int i = 0; i < 6; i++)
            {
                float p = phase + .105f + i * .163f;
                Vector2 tangent;
                Vector2 point = AlongFrame(rect, p, scale, out tangent);
                float size = (i % 3 == 0 ? 1.65f : 1.05f) * scale;
                float alpha = (.38f + .12f * Mathf.Sin(_clock * 1.6f + i * 2.1f)) * strength;
                Diamond(vh, point, tangent, size * 1.7f, size * .64f,
                    new Color(.69f, .88f, .98f, alpha));
            }
        }

        private static void Stream(VertexHelper vh, Rect rect, float head, float length, float scale, float strength)
        {
            const int steps = 32;
            int first = vh.currentVertCount;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 tangent;
                Vector2 point = AlongFrame(rect, head - length * (1 - t), scale, out tangent);
                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                float width = scale * Mathf.Lerp(.15f, .95f, t);
                Color color = new Color(Mathf.Lerp(.44f, .84f, t), Mathf.Lerp(.73f, .94f, t), 1,
                    Mathf.Pow(t, 1.4f) * .68f * strength);
                vh.AddVert(point - normal * width, color, Vector2.zero);
                vh.AddVert(point + normal * width, color, Vector2.zero);
                if (i == 0) continue;
                int a = first + (i - 1) * 2;
                vh.AddTriangle(a, a + 1, a + 2); vh.AddTriangle(a + 1, a + 3, a + 2);
            }
            Vector2 direction;
            Vector2 tip = AlongFrame(rect, head, scale, out direction);
            Diamond(vh, tip, direction, 3.0f * scale, 1.22f * scale, new Color(.86f, .97f, 1, .8f * strength));
        }

        private static void Diamond(VertexHelper vh, Vector2 point, Vector2 tangent, float length, float width, Color color)
        {
            int first = vh.currentVertCount;
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            vh.AddVert(point + tangent * length, color, Vector2.zero);
            vh.AddVert(point + normal * width, color, Vector2.zero);
            vh.AddVert(point - tangent * length, color, Vector2.zero);
            vh.AddVert(point - normal * width, color, Vector2.zero);
            vh.AddTriangle(first, first + 1, first + 2); vh.AddTriangle(first, first + 2, first + 3);
        }

        // Clockwise, arc-length parametrized rounded rectangle, just outside the native frame.
        // Analytic corner tangents keep the head and tail joined while turning.
        private static Vector2 AlongFrame(Rect rect, float phase, float scale, out Vector2 tangent)
        {
            float offset = 1.8f * scale;
            float radius = 5.5f * scale;
            float left = rect.xMin - offset, right = rect.xMax + offset;
            float bottom = rect.yMin - offset, top = rect.yMax + offset;
            float horizontal = right - left - radius * 2;
            float vertical = top - bottom - radius * 2;
            float arc = Mathf.PI * radius * .5f;
            float distance = Mathf.Repeat(phase, 1) * (horizontal * 2 + vertical * 2 + arc * 4);
            for (int side = 0; side < 4; side++)
            {
                float straight = side % 2 == 0 ? horizontal : vertical;
                if (distance <= straight)
                {
                    switch (side)
                    {
                        case 0: tangent = Vector2.right; return new Vector2(left + radius + distance, top);
                        case 1: tangent = Vector2.down; return new Vector2(right, top - radius - distance);
                        case 2: tangent = Vector2.left; return new Vector2(right - radius - distance, bottom);
                        default: tangent = Vector2.up; return new Vector2(left, bottom + radius + distance);
                    }
                }
                distance -= straight;
                if (distance <= arc)
                {
                    float angle = (1 - side) * Mathf.PI * .5f - distance / radius;
                    Vector2 center = new Vector2(side < 2 ? right - radius : left + radius,
                        side == 0 || side == 3 ? top - radius : bottom + radius);
                    tangent = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
                    return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
                distance -= arc;
            }
            tangent = Vector2.right;
            return new Vector2(left + radius, top);
        }
    }
}
