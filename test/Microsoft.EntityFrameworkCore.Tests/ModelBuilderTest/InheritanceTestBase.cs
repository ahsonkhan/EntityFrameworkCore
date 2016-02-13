// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.EntityFrameworkCore.FunctionalTests;
using Microsoft.EntityFrameworkCore.FunctionalTests.TestUtilities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Xunit;

// ReSharper disable once CheckNamespace

namespace Microsoft.EntityFrameworkCore.Tests
{
    public abstract partial class ModelBuilderTest
    {
        public abstract class InheritanceTestBase : ModelBuilderTestBase
        {
            [Fact]
            public virtual void Can_set_and_remove_base_type()
            {
                var modelBuilder = CreateModelBuilder();

                var pickleBuilder = modelBuilder.Entity<Pickle>();
                pickleBuilder.HasOne(e => e.BigMak).WithMany(e => e.Pickles);
                var pickle = pickleBuilder.Metadata;
                // TODO: Remove this line
                // Issue #2837
                modelBuilder.Entity<BigMak>().Ignore(b => b.Bun);

                Assert.Null(pickle.BaseType);
                var pickleClone = modelBuilder.Model.Clone().FindEntityType(pickle.Name);
                var initialProperties = pickleClone.GetProperties();
                var initialKeys = pickleClone.GetKeys();
                var initialIndexes = pickleClone.GetIndexes();
                var initialForeignKeys = pickleClone.GetForeignKeys();
                var initialReferencingForeignKeys = pickleClone.GetReferencingForeignKeys();

                pickleBuilder.HasBaseType<Ingredient>();
                var ingredientBuilder = modelBuilder.Entity<Ingredient>();
                var ingredient = ingredientBuilder.Metadata;

                Assert.Same(typeof(Ingredient), pickle.BaseType.ClrType);
                AssertEqual(initialProperties, pickle.GetProperties(), new PropertyComparer(compareAnnotations: false));
                AssertEqual(initialKeys, pickle.GetKeys());
                AssertEqual(initialIndexes, pickle.GetIndexes());
                AssertEqual(initialForeignKeys, pickle.GetForeignKeys());
                AssertEqual(initialReferencingForeignKeys, pickle.GetReferencingForeignKeys());

                pickleBuilder.HasBaseType(null);

                Assert.Null(pickle.BaseType);
                AssertEqual(initialProperties, pickle.GetProperties(), new PropertyComparer(compareAnnotations: false));
                AssertEqual(initialKeys, pickle.GetKeys());
                AssertEqual(initialIndexes, pickle.GetIndexes());
                AssertEqual(initialForeignKeys, pickle.GetForeignKeys());
                AssertEqual(initialReferencingForeignKeys, pickle.GetReferencingForeignKeys());

                AssertEqual(initialProperties, ingredient.GetProperties(), new PropertyComparer(compareAnnotations: false));
                AssertEqual(initialKeys, ingredient.GetKeys());
                AssertEqual(initialIndexes, ingredient.GetIndexes());
                Assert.Equal(initialForeignKeys.Count(), ingredient.GetForeignKeys().Count());
                Assert.Equal(initialReferencingForeignKeys.Count(), ingredient.GetReferencingForeignKeys().Count());
            }

            [Fact]
            public virtual void Setting_base_type_to_null_fixes_relationships()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Ignore<CustomerDetails>();
                modelBuilder.Ignore<OrderDetails>();
                var principalEntityBuilder = modelBuilder.Entity<Customer>();
                principalEntityBuilder.Ignore(nameof(Customer.Orders));
                var derivedPrincipalEntityBuilder = modelBuilder.Entity<SpecialCustomer>();
                var dependentEntityBuilder = modelBuilder.Entity<Order>();
                var derivedDependentEntityBuilder = modelBuilder.Entity<SpecialOrder>();

                Assert.Same(principalEntityBuilder.Metadata, derivedPrincipalEntityBuilder.Metadata.BaseType);
                Assert.Same(dependentEntityBuilder.Metadata, derivedDependentEntityBuilder.Metadata.BaseType);

                var fk = dependentEntityBuilder.Metadata.GetNavigations().Single().ForeignKey;
                Assert.Equal(nameof(Order.Customer), fk.DependentToPrincipal.Name);
                Assert.Null(fk.PrincipalToDependent);
                Assert.Same(principalEntityBuilder.Metadata, fk.PrincipalEntityType);
                var derivedFk = derivedPrincipalEntityBuilder.Metadata.GetNavigations().Single().ForeignKey;
                Assert.Null(derivedFk.DependentToPrincipal);
                Assert.Equal(nameof(SpecialCustomer.SpecialOrders), derivedFk.PrincipalToDependent.Name);
                Assert.Empty(derivedDependentEntityBuilder.Metadata.GetDeclaredNavigations());
                Assert.Empty(principalEntityBuilder.Metadata.GetNavigations());

                derivedDependentEntityBuilder.HasBaseType(null);

                fk = dependentEntityBuilder.Metadata.GetNavigations().Single().ForeignKey;
                Assert.Equal(nameof(Order.Customer), fk.DependentToPrincipal.Name);
                Assert.Null(fk.PrincipalToDependent);
                Assert.Same(principalEntityBuilder.Metadata, fk.PrincipalEntityType);
                var newDerivedFk = derivedDependentEntityBuilder.Metadata.GetDeclaredNavigations().Single().ForeignKey;
                Assert.Equal(nameof(Order.Customer), newDerivedFk.DependentToPrincipal.Name);
                Assert.Equal(nameof(SpecialCustomer.SpecialOrders), newDerivedFk.PrincipalToDependent.Name);
                Assert.Same(derivedPrincipalEntityBuilder.Metadata, newDerivedFk.PrincipalEntityType);
                Assert.Empty(principalEntityBuilder.Metadata.GetNavigations());
            }

            [Fact]
            public virtual void Pulling_relationship_to_a_derived_type_creates_relationships_on_other_derived_types()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Ignore<CustomerDetails>();
                modelBuilder.Ignore<OrderDetails>();
                var principalEntityBuilder = modelBuilder.Entity<Customer>();
                principalEntityBuilder.Ignore(nameof(Customer.Orders));
                var derivedPrincipalEntityBuilder = modelBuilder.Entity<SpecialCustomer>();
                var dependentEntityBuilder = modelBuilder.Entity<Order>();
                var derivedDependentEntityBuilder = modelBuilder.Entity<SpecialOrder>();
                var otherDerivedDependentEntityBuilder = modelBuilder.Entity<BackOrder>();

                Assert.Same(principalEntityBuilder.Metadata, derivedPrincipalEntityBuilder.Metadata.BaseType);
                Assert.Same(dependentEntityBuilder.Metadata, derivedDependentEntityBuilder.Metadata.BaseType);
                Assert.Same(dependentEntityBuilder.Metadata, otherDerivedDependentEntityBuilder.Metadata.BaseType);

                var fk = dependentEntityBuilder.Metadata.GetNavigations().Single().ForeignKey;
                Assert.Equal(nameof(Order.Customer), fk.DependentToPrincipal.Name);
                Assert.Null(fk.PrincipalToDependent);
                Assert.Same(principalEntityBuilder.Metadata, fk.PrincipalEntityType);
                var derivedFk = derivedPrincipalEntityBuilder.Metadata.GetNavigations().Single().ForeignKey;
                Assert.Null(derivedFk.DependentToPrincipal);
                Assert.Equal(nameof(SpecialCustomer.SpecialOrders), derivedFk.PrincipalToDependent.Name);
                Assert.Empty(derivedDependentEntityBuilder.Metadata.GetDeclaredNavigations());
                Assert.Empty(otherDerivedDependentEntityBuilder.Metadata.GetDeclaredNavigations());
                Assert.Empty(principalEntityBuilder.Metadata.GetNavigations());

                derivedPrincipalEntityBuilder
                    .HasMany(e => e.SpecialOrders)
                    .WithOne(e => (SpecialCustomer)e.Customer);

                Assert.Empty(dependentEntityBuilder.Metadata.GetForeignKeys());
                Assert.Empty(dependentEntityBuilder.Metadata.GetNavigations());
                var newFk = derivedDependentEntityBuilder.Metadata.GetDeclaredNavigations().Single().ForeignKey;
                Assert.Equal(nameof(Order.Customer), newFk.DependentToPrincipal.Name);
                Assert.Equal(nameof(SpecialCustomer.SpecialOrders), newFk.PrincipalToDependent.Name);
                Assert.Same(derivedPrincipalEntityBuilder.Metadata, newFk.PrincipalEntityType);
                var otherDerivedFk = otherDerivedDependentEntityBuilder.Metadata.GetDeclaredNavigations().Single().ForeignKey;
                Assert.Equal(nameof(Order.Customer), otherDerivedFk.DependentToPrincipal.Name);
                Assert.Null(otherDerivedFk.PrincipalToDependent);
                Assert.Equal(nameof(Order.CustomerId), otherDerivedFk.Properties.Single().Name);
            }

            [Fact]
            public virtual void Setting_base_type_handles_require_value_generator_properly()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Ignore<Customer>();
                modelBuilder.Entity<OrderDetails>();
                modelBuilder.Entity<SpecialOrder>();

                Assert.False(modelBuilder.Model.FindEntityType(typeof(OrderDetails)).FindProperty(OrderDetails.OrderIdProperty).RequiresValueGenerator);
            }

            [Fact]
            public virtual void Can_create_relationship_between_base_type_and_derived_type()
            {
                var modelBuilder = CreateModelBuilder();
                var relationshipBuilder = modelBuilder.Entity<BookLabel>()
                    .HasOne(e => e.SpecialBookLabel)
                    .WithOne(e => e.BookLabel)
                    .HasPrincipalKey<SpecialBookLabel>(e => e.Id);

                Assert.NotNull(relationshipBuilder);
                Assert.Equal(typeof(BookLabel), relationshipBuilder.Metadata.DeclaringEntityType.ClrType);
                Assert.Equal(typeof(SpecialBookLabel), relationshipBuilder.Metadata.PrincipalEntityType.ClrType);
                Assert.Equal(nameof(BookLabel.SpecialBookLabel), relationshipBuilder.Metadata.DependentToPrincipal.Name);
                Assert.Equal(nameof(SpecialBookLabel.BookLabel), relationshipBuilder.Metadata.PrincipalToDependent.Name);
                Assert.Equal(nameof(SpecialBookLabel.Id), relationshipBuilder.Metadata.PrincipalKey.Properties.Single().Name);
            }

            [Fact]
            public virtual void Removing_derived_type_make_sure_that_entity_type_is_removed_from_directly_derived_type()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<Book>();
                modelBuilder.Ignore<SpecialBookLabel>();
                modelBuilder.Ignore<AnotherBookLabel>();

                Assert.Empty(modelBuilder.Model.FindEntityType(typeof(BookLabel).FullName).GetDirectlyDerivedTypes());
            }

            [Fact]
            public virtual void Do_not_run_relationship_discovery_on_entity_type_while_removing_it()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Entity<SpecialBookLabel>();
                modelBuilder.Entity<AnotherBookLabel>();
                modelBuilder.Ignore<BookLabel>();

                Assert.Null(modelBuilder.Model.FindEntityType(typeof(BookLabel).FullName));
            }

            [Fact]
            public virtual void Can_reconfigure_inherited_intraHierarchical_relationship()
            {
                var modelBuilder = CreateModelBuilder();
                modelBuilder.Ignore<Book>();
                var bookLabelEntityBuilder = modelBuilder.Entity<BookLabel>();
                bookLabelEntityBuilder.Ignore(e => e.AnotherBookLabel);
                bookLabelEntityBuilder.HasOne(e => e.SpecialBookLabel)
                    .WithOne(e => e.BookLabel)
                    .HasPrincipalKey<BookLabel>(e => e.Id);

                var extraSpecialBookLabelEntityBuilder = modelBuilder.Entity<ExtraSpecialBookLabel>();
                modelBuilder.Entity<SpecialBookLabel>()
                    .HasOne(e => (ExtraSpecialBookLabel)e.SpecialBookLabel)
                    .WithOne(e => (SpecialBookLabel)e.BookLabel);

                var fk = bookLabelEntityBuilder.Metadata.FindNavigation(nameof(BookLabel.SpecialBookLabel)).ForeignKey;
                Assert.Equal(nameof(SpecialBookLabel.BookLabel), fk.DependentToPrincipal.Name);
                Assert.Same(fk, extraSpecialBookLabelEntityBuilder.Metadata.GetForeignKeys().Single());
            }
        }
    }
}
