using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Caching;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Catalog.Models;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Support
{
    [PipelineDisplayName("Catalog.block.GetCustomRelationshipsBlock")]
    public class GetCustomRelationshipsBlock : PipelineBlock<CommerceEntity, CommerceEntity, CommercePipelineExecutionContext>
    {
        private readonly IFindEntitiesInListPipeline _findEntitiesInListPipeline;

        public GetCustomRelationshipsBlock(IFindEntitiesInListPipeline findEntitiesInListPipeline)
            : base((string)null)
        {
            _findEntitiesInListPipeline = findEntitiesInListPipeline;
        }

        public override async Task<CommerceEntity> Run(CommerceEntity commerceEntity, CommercePipelineExecutionContext context)
        {
            Condition.Requires(commerceEntity).IsNotNull($"{base.Name}: The argument cannot be null");
            Type entityType = commerceEntity.GetType();
            IEnumerable<RelationshipDefinition> enumerable = from RelationshipDefinition x in (await _findEntitiesInListPipeline.Run(new FindEntitiesInListArgument(typeof(RelationshipDefinition), context.GetPolicy<KnownRelationshipListsPolicy>().CustomRelationshipDefinitions, 0, int.MaxValue), context)).List.Items
                                                             where x.SourceType.Equals(entityType.GetFullyQualifiedName(), StringComparison.OrdinalIgnoreCase)
                                                             select x;

            RelationshipsComponent relationshipsComponent = new RelationshipsComponent();
            foreach (RelationshipDefinition item in enumerable)
            {
                Relationship obj = new Relationship
                {
                    Name = item.Name
                };

                relationshipsComponent.Relationships.Add(obj);
                Type type = Type.GetType(item.TargetType);
                string listName = $"{item.Name}-{commerceEntity.FriendlyId}";

                foreach (CommerceEntity item2 in _findEntitiesInListPipeline.Run(new FindEntitiesInListArgument(type, listName, 0, int.MaxValue), context).Result.List.Items)
                {
                    CatalogItemBase catalogItemBase = item2 as CatalogItemBase;
                    obj.RelationshipList.Add(catalogItemBase.SitecoreId);
                }
            }

            commerceEntity.SetComponent(relationshipsComponent);

            return commerceEntity;
        }
    }
}
