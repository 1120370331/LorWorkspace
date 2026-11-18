using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

var dll=Path.GetFullPath(args[0]);
var pdbPath=Path.GetFullPath(args[1]);
using var pe=new PEReader(File.OpenRead(dll));
var metadata=pe.GetMetadataReader();
using var provider=MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(pdbPath));
var pdb=provider.GetMetadataReader();
var pdbId=new Guid(pdb.DebugMetadataHeader!.Id.Take(16).ToArray());
var codeViews=pe.ReadDebugDirectory().Where(x=>x.Type==DebugDirectoryEntryType.CodeView)
    .Select(x=>pe.ReadCodeViewDebugDirectoryData(x)).ToArray();
if(!codeViews.Any(x=>x.Guid==pdbId))throw new InvalidOperationException("PDB does not match this DLL");
string Hash(byte[] data)=>Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
var documents=pdb.Documents.Select(h=>pdb.GetDocument(h)).Select(d=>new {
    path=pdb.GetString(d.Name),algorithm=pdb.GetGuid(d.HashAlgorithm),checksum=Convert.ToHexString(pdb.GetBlobBytes(d.Hash)).ToLowerInvariant()
}).ToArray();
var resources=new List<object>();
var resourceDirectory=pe.PEHeaders.CorHeader!.ResourcesDirectory;
if(resourceDirectory.Size>0)
{
    var blob=pe.GetSectionData(resourceDirectory.RelativeVirtualAddress).GetContent().ToArray();
    foreach(var h in metadata.ManifestResources)
    {
        var r=metadata.GetManifestResource(h);if(!r.Implementation.IsNil)continue;
        int offset=checked((int)r.Offset),length=BitConverter.ToInt32(blob,offset);
        resources.Add(new {name=metadata.GetString(r.Name),length,sha256=Hash(blob.AsSpan(offset+4,length).ToArray())});
    }
}
string TypeName(TypeDefinitionHandle handle)
{
    var t=metadata.GetTypeDefinition(handle);var parent=t.GetDeclaringType();
    return parent.IsNil?metadata.GetString(t.Namespace)+"."+metadata.GetString(t.Name):TypeName(parent)+"+"+metadata.GetString(t.Name);
}
var report=new {
    dll,dll_sha256=Hash(File.ReadAllBytes(dll)),pdb=pdbPath,pdb_sha256=Hash(File.ReadAllBytes(pdbPath)),pdbId,pdb_matches=true,
    documents,resources,types=metadata.TypeDefinitions.Select(TypeName).Order().ToArray(),
    references=metadata.AssemblyReferences.Select(h=>metadata.GetAssemblyReference(h)).Select(a=>new{name=metadata.GetString(a.Name),version=a.Version.ToString()}).ToArray()
};
File.WriteAllText(args[2],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"PDB_MATCH documents={documents.Length} resources={resources.Count} output={args[2]}");
