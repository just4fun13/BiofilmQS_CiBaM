using UnityEngine.SceneManagement;

namespace CellularAutomaton
{
    public class SceneMan : GenericSingletonClass<SceneMan>
    {
        public void ReloadScene()
        {
            SceneManager.LoadScene(0);
        }

    }
}
