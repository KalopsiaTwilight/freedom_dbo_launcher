﻿# freedom_client

Repository with source code for the Freedom Client.

The FreedomManifest tool is used to generate a manifest.json file with filenames and SHA1 hashes as well as a PKCS#7 signature for this manifest.json file.

The Freedom Client will request the manifest.json file and signature, verify it is signed by the Freedom Certificate and then download each file listed, verifying the SHA1 hashes before saving them to disk. 

## Requirements for building 

To download google drive files a google credential is needed.
See https://developers.google.com/workspace/guides/create-credentials#service-account

Create a service account, add a credential and use this json file in data/google-credentials.json.
Enable Google Drive in https://console.developers.google.com/apis/api/drive.googleapis.com/ for the created project.

Service account will then have to be given access to the files.
