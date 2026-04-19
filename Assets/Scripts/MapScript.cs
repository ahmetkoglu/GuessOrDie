using UnityEngine;
using UnityEngine.EventSystems;

public class MapZoom : MonoBehaviour, IScrollHandler
{
    [Header("Zoom Ayarları")]
    public RectTransform mapContent; // Büyütüp küçülteceğimiz asıl harita
    public float zoomSpeedPC = 0.1f;
    public float zoomSpeedMobile = 0.01f;
    public float minZoom = 1f; // Harita en fazla ne kadar küçülebilir (1 = Orijinal boyut)
    public float maxZoom = 3f; // Harita en fazla ne kadar büyüyebilir (3 = 3 kat)

    void Update()
    {
        // --- MOBİL: ÇİMDİK (PINCH) İLE ZOOM ---
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // İki parmağın bir önceki frame'deki konumlarını bul
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Parmaklar arasındaki mesafeleri ölç (Eski mesafe vs Yeni mesafe)
            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            // Aradaki farkı bul (Uzaklaşıyorlar mı, yakınlaşıyorlar mı?)
            float difference = currentMagnitude - prevMagnitude;

            ZoomMap(difference * zoomSpeedMobile);
        }
    }

    // --- PC: FARE TEKERLEĞİ İLE ZOOM ---
    public void OnScroll(PointerEventData eventData)
    {
        ZoomMap(eventData.scrollDelta.y * zoomSpeedPC);
    }

    private void ZoomMap(float increment)
    {
        // Scale (Boyut) değerini hesapla ve min-max sınırları içinde tut (Clamp)
        float newScale = Mathf.Clamp(mapContent.localScale.x + increment, minZoom, maxZoom);
        
        // Yeni boyutu haritaya uygula
        mapContent.localScale = new Vector3(newScale, newScale, 1f);
    }
}