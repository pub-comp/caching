namespace PubComp.Caching.AopCaching.UnitTests.Mocks
{
    public class MockObject
    {
        private readonly int data;

        public MockObject(int data)
        {
            this.data = data;
        }

        public override int GetHashCode()
        {
            return data;
        }

        public override bool Equals(object obj)
        {
            var other = obj as MockObject;
            if (other == null)
                return false;

            return other.data != this.data;
        }

        public override string ToString()
        {
            return data.ToString();
        }
    }
}
