﻿
using ME.ECS.Collections;
using Unity.Jobs;

namespace ME.ECS.Tests {

    public class Tests_Filters_FilterBag {

        public struct TestData : IComponent {

            public int a;

        } 

        public struct TestData2 : IComponent {

            public int a;

        } 

        [Unity.Burst.BurstCompileAttribute(Unity.Burst.FloatPrecision.High, Unity.Burst.FloatMode.Deterministic, CompileSynchronously = true)]
        public struct FilterBagJobTest : IJobParallelForFilterBag<ME.ECS.Buffers.FilterBag<TestData, TestData2>> {

            public void Execute(ref ME.ECS.Buffers.FilterBag<TestData, TestData2> bag, int index) {

                var has = bag.HasT1(index);
                if (has == false) {
                    
                    ref var comp = ref bag.GetT0(index);
                    comp.a += 1;

                    bag.RemoveT1(index);

                }

            }

        }

        [Unity.Burst.BurstCompileAttribute(Unity.Burst.FloatPrecision.High, Unity.Burst.FloatMode.Deterministic, CompileSynchronously = true, Debug = false)]
        private class TestJobSystem : ISystem, IAdvanceTick {

            public World world { get; set; }

            public Entity testEntity;

            private Filter filter;
            
            public void OnConstruct() {
                
                this.filter = Filter.Create("Test").With<TestData>().Push();
                
            }

            public void OnDeconstruct() {
                
            }

            public void AdvanceTick(in float deltaTime) {

                {
                    var a = this.testEntity.Read<TestData>().a;
                    
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    sw.Start();
                    var bag = new ME.ECS.Buffers.FilterBag<TestData, TestData2>(this.filter, Unity.Collections.Allocator.TempJob);
                    var job = new FilterBagJobTest() {
                    };
                    var handle = job.Schedule(bag);
                    handle.Complete();
                    sw.Stop();
                    var step1 = sw.ElapsedMilliseconds;

                    sw = System.Diagnostics.Stopwatch.StartNew();
                    sw.Start();
                    bag.Push();

                    sw.Stop();
                    UnityEngine.Debug.Log(step1 + "ms, " + sw.ElapsedMilliseconds + "ms. Entities: " + this.filter.Count + ": " + this.testEntity + " has data: " +
                                          this.testEntity.Read<TestData>().a);
                    
                    UnityEngine.Assertions.Assert.IsTrue(this.testEntity.Read<TestData>().a == a + 1);
                    UnityEngine.Assertions.Assert.IsTrue(this.testEntity.Has<TestData2>() == false);

                }

                /*{
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    sw.Start();
                    this.filter.ForEach((in Entity entity, in TestData2 data2, ref TestData data) => {
                        ++data.a;
                    }).WithBurst().Do();
                    sw.Stop();
                    UnityEngine.Debug.Log(sw.ElapsedMilliseconds + "ms. Entities: " + this.filter.Count + ": " + this.testEntity + " has data: " +
                                          this.testEntity.Read<TestData>().a);
                }*/
                
            }

        }

        [NUnit.Framework.TestAttribute]
        public void FilterBag() {

            TestsHelper.Do((w) => {
                
                ref var str = ref w.GetStructComponents();
                WorldUtilities.InitComponentTypeId<TestData>(isBlittable: true);
                WorldUtilities.InitComponentTypeId<TestData2>(isBlittable: true);
                str.ValidateUnmanaged<TestData>(ref w.GetState().allocator);
                str.ValidateUnmanaged<TestData2>(ref w.GetState().allocator);
                
            }, (w) => {
                
                var testEntity = new Entity("Test");
                var group = new SystemGroup(w, "TestGroup");
                group.AddSystem(new TestJobSystem() { testEntity = testEntity });

                testEntity.Set(new TestData());

                var cap = 10_000;
                w.SetEntitiesCapacity(cap);
                for (int i = 0; i < cap; ++i) {
                
                    var test = new Entity("Test");
                    test.Set(new TestData());
                
                }
            
                for (int i = cap / 50; i < cap / 20; ++i) {

                    var ent = w.GetEntityById(i);
                    ent.Destroy();

                }
                
                w.UpdateFilters(Entity.Empty);

            }, from: 0, to: 10);

        }

    }
}
