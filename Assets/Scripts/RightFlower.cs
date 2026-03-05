using UnityEngine;

public class RF : MonoBehaviour
{
    public SpriteRenderer targetR;
    public Sprite[] texturesR;
    public int idxR;

    public void UpdateR(int x, int y)
    {

        if (x == -1)
        {
            x = 0;
        }
        x = x*10;


        if (y == -1)
        {
            y = 0;
        }


        targetR.sprite = texturesR[x+y];
    }
}
