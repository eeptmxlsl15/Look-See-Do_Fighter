namespace Quantum
{
    //이거 왜한거지 알기
    public partial class RuntimePlayer
    {
        public bool IsLocalPlayer;
        // Immutable after AddPlayer. Simulation copies validated IDs into its Frame.
        public int[] CommandSkills;
    }
}
