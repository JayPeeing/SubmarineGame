using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SharkMonsterController : MonoBehaviour
{
    public GameObject segmentPrefab;
    public GameObject tailPrefab; // New tail sprite prefab
    public float followDuration = 10.0f;
    public float attackDuration = 10.0f;
    public float speed = 2.0f;
    public float straightSpeed = 2.0f;
    public int initialSegmentCount = 5;
    public float segmentSpacing = 0.5f;

    private GameObject tail; // Store a reference to the tail sprite
    private Vector3 tailOffset = new Vector3(-1f, 0f, 0f); // Offset to spawn tail behind the last one
    private GameObject player;

    private float followTimer;
    private float attackTimer;
    private bool isFollowing = true;
    private Vector2 direction;
    public List<Transform> segments = new List<Transform>();

    //For Freeze 
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    public Color freezeColor = Color.red;
    private bool isFrozen = false;
    private float initialSpeed; 
    private float freezeDuration = 2.0f; // Duration of the freeze in seconds

    public int enemyDamage = 10;

    //For Audio
    public AudioClip attackSound;
    private AudioSource audioSource;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        followTimer = followDuration;

        // Create initial segments
        for (int i = 0; i < initialSegmentCount; i++)
        {
            AddSegment();
        }

        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        originalColor = spriteRenderer.color;

    }

    void Update()
    {
        if (isFollowing)
        {
            FollowPlayer();
            followTimer -= Time.deltaTime;

            if (followTimer <= 0)
            {
                attackTimer = attackDuration;
                isFollowing = false;
                direction = transform.right; // Continue in the direction the enemy is facing
                audioSource.PlayOneShot(attackSound); // Play sound
            }
        }
        else
        {
            MoveStraight();
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0)
            {
                isFollowing = true;
                followTimer = followDuration;
            }
        }

        MoveSegments();
        UpdateTailPosition();
    }

    void FollowPlayer()
    {
        if (player != null)
        {
            Vector2 playerPosition = player.transform.position;
            Vector2 enemyPosition = transform.position;
            Vector2 directionToPlayer = (playerPosition - enemyPosition).normalized;

            transform.position = Vector2.MoveTowards(transform.position, playerPosition, speed * Time.deltaTime);

            // Rotate enemy to face the player
            float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
    }

    void MoveStraight()
    {
        transform.Translate(direction * straightSpeed * Time.deltaTime, Space.World);
    }

    void MoveSegments()
    {
        for (int i = segments.Count - 1; i > 0; i--)
        {
            // Follow the position and rotation of the segment in front of it
            segments[i].position = Vector2.Lerp(segments[i].position, segments[i - 1].position - segments[i - 1].right * segmentSpacing, speed * Time.deltaTime / segmentSpacing);

            // Smoothly rotate the segment to match the segment in front of it
            float angle = Mathf.Atan2(segments[i - 1].position.y - segments[i].position.y, segments[i - 1].position.x - segments[i].position.x) * Mathf.Rad2Deg;
            segments[i].rotation = Quaternion.Lerp(segments[i].rotation, Quaternion.Euler(new Vector3(0, 0, angle)), speed * Time.deltaTime / segmentSpacing);
        }

        if (segments.Count > 0)
        {
            segments[0].position = Vector2.Lerp(segments[0].position, transform.position - transform.right * segmentSpacing, speed * Time.deltaTime / segmentSpacing);

            // Smoothly rotate the first segment to match the head's rotation
            float angle = Mathf.Atan2(transform.position.y - segments[0].position.y, transform.position.x - segments[0].position.x) * Mathf.Rad2Deg;
            segments[0].rotation = Quaternion.Lerp(segments[0].rotation, Quaternion.Euler(new Vector3(0, 0, angle)), speed * Time.deltaTime / segmentSpacing);
        }


    }

    void UpdateTailPosition()
    {
        // Ensure there is at least one segment
        if (segments.Count > 0)
        {
            Transform lastSegment = segments[segments.Count - 1];
            Vector3 tailPosition = lastSegment.position - lastSegment.right * segmentSpacing + lastSegment.TransformDirection(tailOffset);
            Quaternion tailRotation = lastSegment.rotation;
            tail.transform.position = tailPosition;
            tail.transform.rotation = tailRotation;
        }
    }

    void AddSegment()
    {
        GameObject newSegment = Instantiate(segmentPrefab);
        if (segments.Count == 0)
        {
            newSegment.transform.position = transform.position - transform.right * segmentSpacing;
            newSegment.transform.rotation = transform.rotation;
        }
        else
        {
            Transform lastSegment = segments[segments.Count - 1];
            newSegment.transform.position = lastSegment.position - lastSegment.right * segmentSpacing;
            newSegment.transform.rotation = lastSegment.rotation;
        }
        segments.Add(newSegment.transform);

        // If this is the last segment, spawn the tail
        if (segments.Count == initialSegmentCount)
        {
            tail = Instantiate(tailPrefab); // Instantiate the tail
            UpdateTailPosition(); // Update tail position initially
        }
    }
    public void Freeze()
    {
        if (!isFrozen)
        {
            isFrozen = true;
            followTimer = 0;
            initialSpeed = straightSpeed;
            straightSpeed = 0;
            spriteRenderer.color = freezeColor; // Change color to red

            // Change color of all segments
            foreach (Transform segment in segments)
            {
                SpriteRenderer segmentRenderer = segment.GetComponent<SpriteRenderer>();
                segmentRenderer.color = freezeColor;
            }

            SpriteRenderer tailRenderer = tail.GetComponent<SpriteRenderer>();
            tailRenderer.color = freezeColor;

            // Start coroutine to unfreeze after a delay
            StartCoroutine(UnfreezeAfterDelay());
        }
    }
    private IEnumerator UnfreezeAfterDelay()
    {
        yield return new WaitForSeconds(freezeDuration);

        // Revert the changes
        isFrozen = false;
        attackTimer = 0;
        straightSpeed = initialSpeed;
        spriteRenderer.color = originalColor;

        // Revert color of all segments
        foreach (Transform segment in segments)
        {
            SpriteRenderer segmentRenderer = segment.GetComponent<SpriteRenderer>();
            segmentRenderer.color = originalColor;
        }

        SpriteRenderer tailRenderer = tail.GetComponent<SpriteRenderer>();
        tailRenderer.color = originalColor;
    }

    public void DestroyAllSegments() 
    {
        foreach (Transform segment in segments)
        {
            Destroy(segment.gameObject);
        }
        segments.Clear();

        Destroy(tail);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerOxygen>().TakeDamage(enemyDamage);
        }

    }
}

