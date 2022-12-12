using Evergine.Mathematics;
using Evergine.Networking;
using Evergine.Networking.Connection.Messages;
using Lidgren.Network;
using Xrv.Core.Networking.Properties.Session;
using Xunit;

namespace Xrv.Core.Tests.Networking.Properties.Session
{
    public class SessionDataShould
    {
        private readonly SessionData data;

        public SessionDataShould()
        {
            data = new SessionData();
        }

        [Fact]
        public void SaveAndRetrieveGroupData()
        {
            const string GroupName = "Group1";
            var expected = new GroupTestData();
            var group = new SessionDataGroup
            {
                GroupName = GroupName,
                GroupData = expected,
            };
            data.SetGroupData(group);
            bool succeeded = data.TryGetGroupData<GroupTestData>(GroupName, out var actual);
            Assert.True(succeeded);
            Assert.Equal(expected.String1, actual.String1);
            Assert.Equal(expected.String2, actual.String2);
            Assert.Equal(expected.Int1, actual.Int1);
            Assert.Equal(expected.SubData.Float1, actual.SubData.Float1);
            Assert.Equal(expected.SubData.Vector2, actual.SubData.Vector2);
        }

        [Fact]
        public void SerializeAndDeserializeData()
        {
            const string GroupName = "Group1";
            var groupData = new GroupTestData();
            var group = new SessionDataGroup
            {
                GroupName = GroupName,
                GroupData = groupData,
            };
            data.SetGroupData(group);

            var buffer = new NetBuffer();
            ((INetworkSerializable)data).Write(buffer);
            buffer.Position = 0;

            var @new = new SessionData();
            ((INetworkSerializable)@new).Read(buffer);
            bool succeeded = data.TryGetGroupData<GroupTestData>(GroupName, out var actual);
            Assert.True(succeeded);
            Assert.Equal(groupData.String1, actual.String1);
            Assert.Equal(groupData.String2, actual.String2);
            Assert.Equal(groupData.Int1, actual.Int1);
            Assert.Equal(groupData.SubData.Float1, actual.SubData.Float1);
            Assert.Equal(groupData.SubData.Vector2, actual.SubData.Vector2);
        }

        private class GroupTestData : INetworkSerializable
        {
            public string String1 { get; set; } = "this is a string";

            public string String2 { get; set; } = "this is other one";

            public int Int1 { get; set; } = 12345;

            public GroupTestSubData SubData { get; set; } = new GroupTestSubData();

            public void Read(NetBuffer buffer)
            {
                String1 = buffer.ReadString();
                String2 = buffer.ReadString();
                Int1 = buffer.ReadInt32();

                SubData = new GroupTestSubData();
                SubData.Read(buffer);
            }

            public void Write(NetBuffer buffer)
            {
                buffer.Write(String1);
                buffer.Write(String2);
                buffer.Write(Int1);
                SubData.Write(buffer);
            }

            public class GroupTestSubData : INetworkSerializable
            {
                public float Float1 { get; set; } = 3.14f;

                public Vector2 Vector2 { get; set; } = Vector2.One;

                public void Read(NetBuffer buffer)
                {
                    Float1 = buffer.ReadFloat();
                    Vector2 = buffer.ReadVector2();
                }

                public void Write(NetBuffer buffer)
                {
                    buffer.Write(Float1);
                    buffer.Write(Vector2);
                }
            }
        }
    }
}
