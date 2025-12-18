using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ArenaController : MonoBehaviour
{
    [System.Serializable]
    public class ArenaPiece
    {
        public string pieceName = "Arena Piece";
        public GameObject pieceObject;

        [Header("Timing")]
        [Range(0f, 1f)]
        public float fallTimePercent = 0.5f;   // When to fall (0–1 of match)
        public float fallTimeSeconds;           // Absolute time fallback
        public bool usePercentage = true;

        [Header("Fall Settings")]
        public float fallDuration = 2f;         // Duration in seconds
        public float fallDistance = 20f;
        public bool destroyAfterFall = true;
        public float destroyDelay = 2f;

        [Space]
        //public UnityEvent onPieceFall;

        [HideInInspector] public bool hasFallen = false;
        [HideInInspector] public Vector3 originalPosition;
        [HideInInspector] public Quaternion originalRotation;
    }

    [Header("Arena Settings")]
    [SerializeField] private float matchDuration = 240f;
    [SerializeField] private List<ArenaPiece> arenaPieces = new();

    [Header("Timing Options")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private float startDelay = 0f;

    [Header("Warning System")]
    [SerializeField] private bool enableWarnings = true;
    [SerializeField] private float warningTime = 3f;
    //[SerializeField] private UnityEvent<int> onPieceWarning;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Runtime
    private float matchTimer = 0f;
    private bool matchActive = false;
    private Coroutine matchCoroutine;

    // Properties
    public float MatchProgress => matchDuration > 0f ? matchTimer / matchDuration : 0f;
    public float TimeRemaining => Mathf.Max(0f, matchDuration - matchTimer);
    public bool IsMatchActive => matchActive;

    private void Awake()
    {
        // Cache original transforms
        foreach (var piece in arenaPieces)
        {
            if (piece.pieceObject != null)
            {
                piece.originalPosition = piece.pieceObject.transform.position;
                piece.originalRotation = piece.pieceObject.transform.rotation;
            }
        }
    }

    private void Start()
    {
        if (autoStart)
            StartMatch(startDelay);
    }

    // ------------------------------------------------------
    // MATCH CONTROL
    // ------------------------------------------------------

    public void StartMatch(float delay = 0f)
    {
        if (matchCoroutine != null)
            StopCoroutine(matchCoroutine);

        matchCoroutine = StartCoroutine(MatchSequence(delay));
    }

    public void StopMatch()
    {
        if (matchCoroutine != null)
        {
            StopCoroutine(matchCoroutine);
            matchCoroutine = null;
        }

        matchActive = false;
        matchTimer = 0f;
    }

    public void ResetArena()
    {
        StopMatch();

        foreach (var piece in arenaPieces)
        {
            piece.hasFallen = false;

            if (piece.pieceObject == null)
                continue;

            piece.pieceObject.SetActive(true);
            piece.pieceObject.transform.position = piece.originalPosition;
            piece.pieceObject.transform.rotation = piece.originalRotation;
        }
    }

    // ------------------------------------------------------
    // MATCH LOOP
    // ------------------------------------------------------

    private IEnumerator MatchSequence(float delay)
    {
        matchTimer = 0f;
        matchActive = false;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        matchActive = true;

        bool[] warningsSent = new bool[arenaPieces.Count];

        while (matchTimer < matchDuration && matchActive)
        {
            matchTimer += Time.deltaTime;

            for (int i = 0; i < arenaPieces.Count; i++)
            {
                var piece = arenaPieces[i];

                if (piece.hasFallen || piece.pieceObject == null)
                    continue;

                float targetTime = piece.usePercentage
                    ? piece.fallTimePercent * matchDuration
                    : piece.fallTimeSeconds;

                // Warning
                if (enableWarnings && !warningsSent[i] && matchTimer >= targetTime - warningTime)
                {
                    warningsSent[i] = true;
                    //onPieceWarning?.Invoke(i);

                    if (debugMode)
                        Debug.Log($"Warning: {piece.pieceName} will fall in {warningTime} seconds");
                }

                // Trigger fall
                if (matchTimer >= targetTime)
                {
                    TriggerPieceFall(piece);
                }
            }

            yield return null;
        }

        matchActive = false;

        if (debugMode)
            Debug.Log("Match complete");
    }

    // ------------------------------------------------------
    // FALL LOGIC
    // ------------------------------------------------------

    private void TriggerPieceFall(ArenaPiece piece)
    {
        if (piece.hasFallen || piece.pieceObject == null)
            return;

        piece.hasFallen = true;
        //piece.onPieceFall?.Invoke();

        if (debugMode)
            Debug.Log($"{piece.pieceName} is falling");

        StartCoroutine(FallSequence(piece));
    }

    private IEnumerator FallSequence(ArenaPiece piece)
    {
        Transform t = piece.pieceObject.transform;
        Vector3 startPos = t.position;
        Vector3 endPos = startPos + Vector3.down * piece.fallDistance;

        float elapsed = 0f;

        while (elapsed < piece.fallDuration)
        {
            elapsed += Time.deltaTime;
            float t01 = Mathf.Clamp01(elapsed / piece.fallDuration);

            // Ease-in fall
            t01 *= t01;

            t.position = Vector3.Lerp(startPos, endPos, t01);
            yield return null;
        }

        if (piece.destroyAfterFall)
        {
            yield return new WaitForSeconds(piece.destroyDelay);
            Destroy(piece.pieceObject);
        }
    }

    // ------------------------------------------------------
    // EXTERNAL CONTROL
    // ------------------------------------------------------

    public void ForcePieceFall(int index)
    {
        if (index >= 0 && index < arenaPieces.Count)
            TriggerPieceFall(arenaPieces[index]);
    }

    public void SetMatchDuration(float seconds)
    {
        matchDuration = Mathf.Max(0.1f, seconds);
    }

    // ------------------------------------------------------
    // GIZMOS
    // ------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (!debugMode) return;

        foreach (var piece in arenaPieces)
        {
            if (piece.pieceObject == null)
                continue;

            Gizmos.color = piece.hasFallen ? Color.red : Color.green;
            Gizmos.DrawWireCube(piece.pieceObject.transform.position, Vector3.one * 0.5f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                piece.pieceObject.transform.position,
                piece.pieceObject.transform.position + Vector3.down * piece.fallDistance
            );
        }
    }
}
