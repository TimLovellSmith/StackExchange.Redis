﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Redis.Tests
{
    public class ConnectToUnexistingHost : TestBase
    {
        public ConnectToUnexistingHost(ITestOutputHelper output) : base (output) { }

        [Fact]
        public async Task FailsWithinTimeout()
        {
            // Avoiding blocking the thread a long time with a failing Connect() call    
            await RunTestOnNewThreadAsync(async () =>
            {
                const int timeout = 1000;
                var sw = Stopwatch.StartNew();
                try
                {
                    var config = new ConfigurationOptions
                    {
                        EndPoints = { { "invalid", 1234 } },
                        ConnectTimeout = timeout
                    };

                    using (ConnectionMultiplexer.Connect(config, Writer))
                    {
                        await Task.Delay(10000).ForAwait();
                    }

                    Assert.True(false, "Connect should fail with RedisConnectionException exception");
                }
                catch (RedisConnectionException)
                {
                    var elapsed = sw.ElapsedMilliseconds;
                    Log("Elapsed time: " + elapsed);
                    Log("Timeout: " + timeout);
                    Assert.True(elapsed < 9000, "Connect should fail within ConnectTimeout, ElapsedMs: " + elapsed);
                }
            });
        }

        [Fact]
        public async Task CanNotOpenNonsenseConnection_IP()
        {
            // Avoiding blocking the thread a long time with a failing Connect() call    
            await RunTestOnNewThreadAsync(() =>
            {
                var ex = Assert.Throws<RedisConnectionException>(() =>
                {
                    using (ConnectionMultiplexer.Connect(TestConfig.Current.MasterServer + ":6500,connectTimeout=1000", Writer)) { }
                });
                Log(ex.ToString());
            });
        }

        [Fact]
        public async Task CanNotOpenNonsenseConnection_DNS()
        {
            // This one uses *ConnectAsync* which hopefully doesn't block the thread (but we will see...)
            var ex = await Assert.ThrowsAsync<RedisConnectionException>(async () =>
            {
                using (await ConnectionMultiplexer.ConnectAsync($"doesnot.exist.ds.{Guid.NewGuid():N}.com:6500,connectTimeout=1000", Writer).ForAwait()) { }
            }).ForAwait();
            Log(ex.ToString());
        }

        [Fact]
        public async Task CreateDisconnectedNonsenseConnection_IP()
        {
            // Avoiding blocking the thread a long time with a failing Connect() call
            await RunTestOnNewThreadAsync(() =>
            {
                using (var conn = ConnectionMultiplexer.Connect(TestConfig.Current.MasterServer + ":6500,abortConnect=false,connectTimeout=1000", Writer))
                {
                    Assert.False(conn.GetServer(conn.GetEndPoints().Single()).IsConnected);
                    Assert.False(conn.GetDatabase().IsConnected(default(RedisKey)));
                }
            });
        }

            [Fact]
        public async Task CreateDisconnectedNonsenseConnection_DNS()
        {
            // Avoiding blocking the thread a long time with a failing Connect() call
            await RunTestOnNewThreadAsync(() =>
            {
                using (var conn = ConnectionMultiplexer.Connect($"doesnot.exist.ds.{Guid.NewGuid():N}.com:6500,abortConnect=false,connectTimeout=1000", Writer))
                {
                    Assert.False(conn.GetServer(conn.GetEndPoints().Single()).IsConnected);
                    Assert.False(conn.GetDatabase().IsConnected(default(RedisKey)));
                }
            });
        }
    }
}
