using Xunit;

namespace NotiHeart.Tests;

[CollectionDefinition("integration")]
public sealed class TestcontainersCollection : ICollectionFixture<TestcontainersFixture>;
