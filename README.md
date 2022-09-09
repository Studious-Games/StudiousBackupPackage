
# Studious Pooling System
 
 Is a small very simple library, that helps Pool Objects. 
 
 Why another Object Pooling System? 

I wrote this a long time ago for my projects, as there was nothing that would work the way I needed a pooling system to work. I have since modified it slightly and decided that it would be good to share it with others.

So what makes this Object Pool any different to others, well others focus more on a GameObject rather than be generic. What that means is you can instantiate a prefab by its script rather than its Game Object, then store that in the Object Pool instead of the GameObject. This helps reduce having to use GetComponents, when everything from the GameObject to Transform will be stored.


## Installation

Studious Pooling System is best installed as a package, this can be done in a number of ways, the best is to use git and install via the package manage.

###### **Via Git Hub** 

We highly recommend installing it this way, if you have git installed on your machine, then you can simply go to main page of this repository and select the code button. Then copy the HTTPS URL using the button next to the URL.

You can then open up the Package Manager within the Unity Editor, and select the drop down on the plus icon along the top left of the Package Manager. You can then select add by Git and paste the URL from the GitHub repository here.

###### **Add Package from Disk** 

There are two ways to do this, one would be to unzip the download to an area on your hard drive, and another would be to unzip the download into the actual project in the packages folder.

Both are workable solutions, however we would suggest if you would like to use this for multiple projects, that you install somewhere on your hard drive that you will remember. And then use the Package Manager to add the package from Disk.

Installing or unzipping into the projects package folder, will work out of the box when you open the project up.

## Usage

Getting started with this pooling system is rather easy, all you need to do is replace certain aspects of your code with entry points into this Object Pool.

For example, in an FPS you might have a Gun Script like the following, where a bullet is instantiated by its script rather than a GameObject.

```CS

public void class BulletSpawn : MonoBehaviour
{
    [SerializeField] private Bullet _bulletPrefab;
    [SerializeField] private Transform _spawnPosition;

    private void Update()
    {
        if (CanFire())
        {
            Instantiate(_bulletPrefab, _spawnPosition.position, _spawnPosition.transform.parent.rotation);
        }
    }
}

```

And a typical bullet script might look something like this

```CS
public class Bullet : MonoBehaviour
{
    [SerializeField] private float _aliveTime;
    [SerializeField] private float _movementSpeed;

    private void Awake()
    {
        Destroy(gameObject, _aliveTime);
    }

    private void Update()
    {
        transform.position += transform.forward * Time.deltaTime * _movementSpeed;
    }
}
```

To make these scripts use the Object Pooling system, we can make small changes to the scripts, first lets make a change to the BulletSpawn script, by adding the following code.

```CS
using Studious.Pooling;

public class BulletSpawn : MonoBehaviour
{
    private ObjectPool<Bullet> _objectPool;

    private void Awake()
    {
        _objectPool = new ObjectPool<Bullet>(_bulletPrefab, 10);
    }
}
```

This change allows the pool to be setup, where we pass in the prefab, and the quantity that we wish to instantiate into the pool before we begin to use it. We now need to make one more change to the BulletSpawn script, so that we use the ObjectPool and not Instantiate.

And we make the following change to the script.

```CS
    if (CanFire())
    {
        _objectPool.Pull(_spawnPosition.position, _spawnPosition.transform.parent.rotation);
    }
```

As you can see the replacement is very simple and almost identical to how you would Instantite a prefab. The two parameters are self explanatory, and are the position and rotation that you want the object to appear and the rotation it should have.

The Bullet script is a little bit different, and we would now write it as the following script.

```CS
using System;
using System.Collections;
using UnityEngine;

//Interface used to make sure required methods are implemented
public class Bullet : MonoBehaviour, IPoolable<Bullet>
{
    [SerializeField] private float _aliveTime;
    [SerializeField] private float _movementSpeed;

    private WaitForSeconds _destroyTime;
    private Action<Bullet> _returnToPool;

    //Is called when the object is removed from the pool, and setups
    // a callback return for returning the object back to the pool
    public void Initialize(Action<Bullet> returnAction)
    {
        _returnToPool = returnAction;
        StartCoroutine(KeepAlive());
    }

    //Callback used for returning the object back to the pool.
    public void ReturnToPool()
    {
        _returnToPool?.Invoke(this);
    }

    private void Awake()
    {
        _destroyTime = new WaitForSeconds(5.0f);
    }

    private void Update()
    {
        transform.position += transform.forward * Time.deltaTime * _movementSpeed;
    }

    //Keeps the Bullet alive before returning it back to the Object Pool.
    private IEnumerator KeepAlive()
    {
        yield return _destroyTime;
        ReturnToPool();
    }
}
```

The code here is self explanatory, and ensures that the bullet stays alive for the time it is required, naturally you would add the code to then check if it hits what it needs to hit, and then return it to the pool. THe example above is just an example that keep it alive for a period of time before we return it back to the Object Pool.


