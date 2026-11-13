using Microsoft.AspNetCore.Mvc;
using SmartAPI.Models;

namespace SmartXApi.Controllers;

[ApiController]
[Route("api/facility")]
public class FacilityController : ControllerBase
{
    [HttpGet("demo")]
    public IActionResult Demo()
    {
        var leaf = FacilityNode.BuildDemoTree();

        var path = new List<string>();
        var depth = 0;
        var cursor = leaf;
        while (cursor != null)
        {
            path.Add(cursor.Name);
            depth++;
            cursor = cursor.Parent;
        }

        return Ok(new
        {
            isValid = FacilityNode.Validate(leaf),
            depthWalked = depth,
            path,
            batchSizesAtLeaf = leaf.Batches.Select(b => b.Length).ToList()
        });
    }
}
