using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using static Hoodie.GroupMaps.Tests.Helpers;
using static Hoodie.GroupMaps.Tests.MapLang.Runner;

namespace Hoodie.GroupMaps.Tests
{
    public class ImmutableSortedSetTests
    {
        [Test]
        public void HashMystery1()
        {
            var set = ImmutableSortedSet<Dummy>.Empty
                .Add(new Dummy(13))
                .Add(new Dummy(11));

            Assert.That(set.Count, Is.EqualTo(2));
        }
        
        [Test]
        public void HashMystery2()
        {
            var set = ImmutableSortedSet<Dummy>.Empty
                .Add(new Dummy(13))
                .Add(new Dummy(13));

            Assert.That(set.Count, Is.EqualTo(2));
        }
        
        [Test]
        public void HashMystery3()
        {
            var set = new SortedSet<Dummy>();
            set.Add(new Dummy(hash: 13));
            set.Add(new Dummy(hash: 13));

            Assert.That(set.Count, Is.EqualTo(2));
        }

        public class Dummy : IComparable<Dummy>
        {
            static int _nextUniqueId = 0;

            readonly int _id = Interlocked.Increment(ref _nextUniqueId);
            readonly int _hash;

            public Dummy(int hash)
            {
                _hash = hash;
            }

            public override int GetHashCode()
                => _hash;

            public int CompareTo(Dummy other)
            {
                return _id.CompareTo(other._id);
            }
        }
        
    }
    
    
    public class DisjunctionTests
    {
        [Test]
        public void SomeTest()
        {
            var d = Run<Disjunction<int, Sym>>(@"
                        A ^ B
                    ");
            
            TestContext.WriteLine(d);
            
            Assert.That(d.Disjuncts, Has.Count.EqualTo(2));
        }
        
    }
    
    
    public class MapIndexAndDisjunctTests
    {
        [Test]
        public void SimpleMap_IsIndexed()
        {
            var map = Map(1, 'A');
            var found = map[1];

            Assert.That(found.Count(), Is.EqualTo(1));
            Assert.That(found.Single().Nodes, Is.EquivalentTo(new[] { 1 }));
            Assert.That(found.Single().Value, Is.EqualTo((Sym)'A'));
        }
        
        [Test]
        public void SimpleMap_HasNoDisjuncts()
        {
            var map = Map(1, 'A');
            var groups = map[1];
            Assert.That(groups.Count(), Is.EqualTo(1));

            var group = groups.Single();
            Assert.That(group.Disjuncts, Is.Empty);
        }
        
        [Test]
        public void SummedMap_IsIndexed()
        {
            var map = Map(1, 'A').Add(Map(2, 'B'));

            var found1 = map[1];
            Assert.That(found1.Count(), Is.EqualTo(1));
            
            var found2 = map[2];
            Assert.That(found2.Count(), Is.EqualTo(1));
        }
        
        [Test]
        public void SummedMap_HasDisjuncts()
        {
            var map = Map(1, 'A').Add(Map(1, 'B'));
            
            var groups = map[1];
            Assert.That(groups.Count(), Is.EqualTo(2));
            
            var group1 = groups.ElementAt(0);
            Assert.That(group1.Disjuncts.Count(), Is.EqualTo(1));

            var group2 = groups.ElementAt(1);
            Assert.That(group2.Disjuncts.Count(), Is.EqualTo(1));
        }
    }

    public class MapCombinationTests
    {
        [Test]
        public void EmptyEmpty_AndEmpty()
        {
            var map1 = EmptyMap;
            var map2 = EmptyMap;

            var combined = map1.Combine(map2);
            Assert.That(combined, Is.EqualTo(EmptyMap));
        }

        [Test]
        public void EmptyOne_AndOne()
        {
            var map1 = EmptyMap;
            var map2 = Map(1, 'A');

            var combined = map1.Combine(map2);
            Assert.That(combined, Is.EqualTo(map2));
        }

        [Test]
        public void OneEmpty_AndOne()
        {
            var map1 = Map(1, 'A');
            var map2 = EmptyMap;

            var combined = map1.Combine(map2);
            Assert.That(combined, Is.EqualTo(map1));
        }

        [Test]
        public void Combine_Empties()
            => Test(@"
                . . . . .
                . * A = A
            ");
        
        [Test]
        public void Combine_Overlaps()
            => Test(@"
                A . . . AB
                A * B = AB
                . . B . AB
                ");
        
        //why does the above not have identity available? but the below does...?
        //it's as if identity is available, only it is treated as worthless, the ever-present identity
        //yet the one below is for some reason meaningful
        
        //here's a question, what happens between 0 and 0
        //
        //
        //we should think of combination as a two-stage process, first of addition, then of a kind of settling and commingling
        //we have two separate binary relations: firstly monoidal addition, and secondly another kind of combination
        //
        //

        [Test]
        public void Combine_NonOverlaps()
            => Test(@"
                A . . . A
                A * . = A
                . . B . B
                ");

        [Test]
        public void Combine_Disjuncts()
            => Test(@"
                A B . C . AC BC
                A . * . = AC .
                . B . C . AC BC
            ");

        [Test]
        public void Combine_ComplexDisjuncts1()
            => Test(@"
                A . . D . ACD .
                A B * . = ACD B
                . B . . . .   B
                . C . D . ACD .
            ");
        
        [Test]
        public void Combine_ComplexDisjuncts2()
            => Test(@"
                A . . . D . ACD BD
                A B . * . = ACD BD
                . B . . . . .   BD 
                . B C . D . ACD BD
            ");
        
        [Test]
        public void Combine_ComplexDisjuncts3()
            => Test(@"
                A . . . D . ABD ACD 
                . B . * . = ABD .
                . B C . D . ABD ACD
            ");

        [Test]
        public void Combine_RhsOnly1()
            => Test(@"
                . * A = A
            ");
        
        [Test]
        public void Combine_RhsOnly2()
            => Test(@"
                . * A B = A B
            ");
        
        [Test]
        public void Combine_DuplexDisjuncts1()
            => Test(@"
                . . . C . .  AC 
                A * B C = AB AC
                A . . . . AB AC
            ");
        
        [Test]
        public void Combine_DuplexDisjuncts2()
            => Test(@"
                A . . . D . A AD .  BD 
                . B * C D = C AD BC BD
                A B . . . . A AD BC BD
            ");
        
        [Test]
        public void Combine_DuplexDisjuncts3()
            => Test(@"
                A B * C D = AC AD BC BD
            ");
        
        [Test]
        public void Combine_DuplexDisjuncts4()
            => Test(@"
                A . . C . AC .
                . B * . = .  B
            ");
        
        [Test]
        public void Combine_DuplexDisjuncts5()
            => Test(@"
                A . . C . AC C
                . B * . = .  B
                A D . . . AC D
            ");
        
        [Test]
        public void Combine_DuplexDisjuncts6()
            => Test(@"
                A B . . . AC B
                A . * C = AC C
            ");
        
        [Test]
        public void Combine_DuplexDisjuncts7()
            => Test(@"
                A B . . . AC BC
                A B * C = AC BC
            ");
        
        [Test]
        public void Combine_DuplexDisjuncts8()
            => Test(@"
                A B . . E . AE BE E
                A . C * . = AE .  C
                . B D . . . .  BE D
            ");
        //above, both A and B are fractured by their off-set disjuncts
        //but the identities they admit are indistinct, they group together happily into one single possible

        [Test]
        public void AggregateTest()
        {
            var result = new int[0].Aggregate(1, (ac, _) => ac);
            Assert.That(result, Is.EqualTo(1));
        }
    }

    public class MapTests
    {
        [Test]
        public void Group_Equality()
        {
            var g1 = Group((1, 2), 'A');
            var g2 = Group((1, 2), 'A');
            Assert.That(g2, Is.EqualTo(g1));

            var h1 = ImmutableHashSet<SimpleGroup<int, Sym>>
                .Empty.Add(g1);
            var h2 = ImmutableHashSet<SimpleGroup<int, Sym>>
                .Empty.Add(g1);

            Assert.That(h1, Is.EqualTo(h2));
            Assert.That(h1.SetEquals(h2));
        }

        [Test]
        public void GroupMap_Equality1()
        {
            var map1 = Map(1, 'A');
            var map2 = Map(1, 'A');
            Assert.That(map2, Is.EqualTo(map1));
        }

        [Test]
        public void GroupMap_Equality2()
        {
            var map1 = Map((1, 2), 'A');
            var map2 = Map((1, 2), 'A');
            Assert.That(map2, Is.EqualTo(map1));
        }


        [Test]
        public void AddingRemoving()
        {
            var m1 = EmptyMap
                .Add(Map((1, 2), 'A'));

            var m2 = EmptyMap
                .Add(Map((2, 3), 'B'))
                .Add(Map((1, 2), 'A'));

            var m3 = m2
                .Remove(m2[3].First());

            Assert.That(m3, Is.EqualTo(m1)
                .Using(MapEqualityComparer.Instance));
        }

        [Test]
        public void AddingRemoving_Indices()
        {
            var map1 = Map((1, 2), 'A');
            var map2 = Map((2, 3), 'B');

            var map3 = EmptyMap
                .Add(map1)
                .Add(map2);

            Assert.Multiple(() =>
            {
                Assert.That(map3[1].Simple(), Is.EquivalentTo(new[] { Group((1, 2), 'A') }));
                Assert.That(map3[2].Simple(), Is.EquivalentTo(new[] { Group((1, 2), 'A'), Group((2, 3), 'B') }));
                Assert.That(map3[3].Simple(), Is.EquivalentTo(new[] { Group((2, 3), 'B') }));
            });

            var map4 = map3.Remove(map1[1].First());

            Assert.Multiple(() =>
            {
                Assert.That(map4[1], Is.Empty);
                Assert.That(map4[2].Simple(), Is.EquivalentTo(new[] { Group((2, 3), 'B') }));
                Assert.That(map4[3].Simple(), Is.EquivalentTo(new[] { Group((2, 3), 'B') }));
            });
        }

        [Test]
        public void SimpleEquality()
            => Test(@"
                 A . A
                 A = A
             ");

        [Test]
        public void Equality_OfDisjuncts1()
            => Test(@"
                 A B . B A
                 A B = B A
             ");
        
        [Test]
        public void Equality_OfDisjuncts2()
            => Test(@"
                 A . . A
                 . B = B
             ");

        [Test]
        public void Equality_OfDisjuncts3()
            => Test(@"
                 A . . A .
                 . B = B .
                 C D . C D
             ");
        
        [Test]
        public void Inequality_Simple1()
            => Test(@"
                 A != B
             ");
        
        [Test]
        public void Inequality_Simple2()
            => Test(@"
                 A != .
             ");
        
        [Test]
        public void Inequality_Simple3()
            => Test(@"
                 A != .
                 . |  A
             ");
        
        [Test]
        public void Inequality_Simple4()
            => Test(@"
                 A . != A B
                 . B |  . B
             ");

        [Test]
        public void Hashes1()
        {
            var m1 = BuildMap("A");
            var m2 = BuildMap("B");
            
            Assert.That(m2.GetHashCode(), Is.Not.EqualTo(m1.GetHashCode()));
        }

        [Test]
        public void Disjuncts_Created()
        {
            var map = BuildMap("A B");

            Assert.Multiple(() =>
            {
                var group1 = map.Groups.ElementAt(0);
                Assert.That(group1.Disjuncts, Has.Count.EqualTo(1));

                var group2 = map.Groups.ElementAt(1);
                Assert.That(group2.Disjuncts, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public void BuildsMap()
        {
            var map = BuildMap(@"
                A B
                A .
                . B
            ");

            Assert.Multiple(() =>
            {
                Assert.That(map[1].Simple(), Is.EquivalentTo(new[]
                {
                    Group((1, 2), 'A'),
                    Group((1, 3), 'B')
                }));

                Assert.That(map[2].Simple(), Is.EquivalentTo(new[]
                {
                    Group((1, 2), 'A'),
                }));

                Assert.That(map[3].Simple, Is.EquivalentTo(new[]
                {
                    Group((1, 3), 'B')
                }));
            });
        }

        Map<int, Sym> BuildMap(string code)
            => Run<Map<int, Sym>>(code);
    }

    public static class TestExtensions
    {
        public static SimpleGroup<int, Sym> Simple(this Group<int, Sym> group)
            => SimpleGroup.From(group.Nodes, group.Value);

        public static IEnumerable<SimpleGroup<int, Sym>> Simple(this IEnumerable<Group<int, Sym>> groups)
            => groups.Select(g => g.Simple());

    }
}