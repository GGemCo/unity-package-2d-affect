using GGemCo2DCore;

namespace GGemCo2DAffect
{
    public class TableLoaderManagerAffect : TableLoaderBase
    {
        /// <summary>
        /// <see cref="TableLoaderManagerAffect"/>의 전역 접근을 위한 Singleton 인스턴스입니다.
        /// </summary>
        public static TableLoaderManagerAffect Instance;

        public TableAffect TableAffect { get; private set; } = new TableAffect();
        public TableAffectModifier TableAffectModifier { get; private set; } = new TableAffectModifier();

        protected void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // 테이블 간 참조 및 의존성 해결을 위해 등록 순서가 중요합니다.
                registry = new TableRegistry();
                registry.Register(TableAffectModifier);
                registry.Register(TableAffect);
            }
            else
            {
                // 이미 Singleton 인스턴스가 존재하는 경우 중복 생성을 방지합니다.
                Destroy(gameObject);
            }
        }
    }
}
