# freedom_client

Repository with source code for the Freedom Client.

The FreedomManifest tool is used to generate a manifest.json file with filenames and SHA1 hashes as well as a PKCS#7 signature for this manifest.json file.

The Freedom Client will request the manifest.json file and signature, verify it is signed by the Freedom Certificate and then download each file listed, verifying the SHA1 hashes before saving them to disk. 

Patching/updating to be implemented.