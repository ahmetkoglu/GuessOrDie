using UnityEngine;
using UnityEngine.EventSystems;

/// <summary> Enables pinching and scrolling logic for map navigation. </summary>
public class MapZoom : MonoBehaviour, IScrollHandler
{
    [Header("Zoom Ayarları")]
    public RectTransform mapContent; 
    public float zoomSpeedPC = 0.1f;
    public float zoomSpeedMobile = 0.01f;
    public float minZoom = 1f; 
    public float maxZoom = 3f; 

    private void Update()
    {
        // --- MOBİL: ÇİMDİK (PINCH) İLE ZOOM ---
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            ZoomMap(difference * zoomSpeedMobile);
        }
    }

    // --- PC: FARE TEKERLEĞİ İLE ZOOM ---
    public void OnScroll(PointerEventData eventData)
    {
        ZoomMap(eventData.scrollDelta.y * zoomSpeedPC);
    }

    /// <summary> Applies math clamping to restrict map sizing boundaries. </summary>
    private void ZoomMap(float increment)
    {
        float newScale = Mathf.Clamp(mapContent.localScale.x + increment, minZoom, maxZoom);
        mapContent.localScale = new Vector3(newScale, newScale, 1f);
    }
}