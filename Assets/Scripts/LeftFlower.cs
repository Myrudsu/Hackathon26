using UnityEngine;

public class LF : MonoBehaviour
{
    public SpriteRenderer targetL;
    public Sprite[] texturesL;
    public int idxL;


    public void UpdateL(int x)
    {
        if (x == -1)
        {
            x = 0;
        }

        targetL.sprite = texturesL[x];
    }
}
