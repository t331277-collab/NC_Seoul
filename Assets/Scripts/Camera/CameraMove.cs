using System.Collections;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    private Vector3 initialPosition;
    private Coroutine resetCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        initialPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        const float moveSpeed = 0.4f;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (ResourceDetailPanelManager.TryCloseOpenPanel())
            {
                return;
            }

            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
            }

            resetCoroutine = StartCoroutine(MoveToInitialPosition());
            return;
        }

        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            move.z += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            move.z -= 1f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            move.x -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            move.x += 1f;
        }

        transform.position += move * moveSpeed * Time.deltaTime;
    }


    private IEnumerator MoveToInitialPosition()
    {
        const float resetDuration = 0.5f;

        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < resetDuration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(startPosition, initialPosition, elapsedTime / resetDuration);
            yield return null;
        }

        transform.position = initialPosition;
        resetCoroutine = null;
    }
}
