using UnityEngine;

public class RF : MonoBehaviour
{
    public SpriteRenderer targetR;
    public Sprite[] texturesR;
    public int idxR;

    public void UpdateR(int x, int y)
    {
        targetR.sprite = texturesR[(x*10)+y];
    }
}
