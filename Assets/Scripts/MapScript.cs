using UnityEngine;
using UnityEngine.EventSystems;

/// <summary> Sadece sürükleme ve manuel X/Y sınırlandırması içerir. </summary>
public class MapDrag : MonoBehaviour, IDragHandler 
{
    [Header("Harita Ayarları")]
    public RectTransform mapContent; 

    [Header("Sınır (Limit) Ayarları")]
    [Tooltip("Haritanın sağa ve sola maksimum kayma mesafesi")]
    public float limitX = 500f; 
    
    [Tooltip("Haritanın aşağı ve yukarı maksimum kayma mesafesi")]
    public float limitY = 800f; 

    public void OnDrag(PointerEventData eventData)
    {
        // 1. Haritayı parmağın hareketi kadar kaydır
        mapContent.anchoredPosition += eventData.delta;
        
        // 2. Anlık pozisyonu değişkene al
        Vector2 currentPos = mapContent.anchoredPosition;

        // 3. X ve Y eksenindeki hareketi, senin belirlediğin limitlerin arasına hapset
        currentPos.x = Mathf.Clamp(currentPos.x, -limitX, limitX);
        currentPos.y = Mathf.Clamp(currentPos.y, -limitY, limitY);

        // 4. Sınırlandırılmış (taşması engellenmiş) pozisyonu haritaya geri ver
        mapContent.anchoredPosition = currentPos;
    }
}