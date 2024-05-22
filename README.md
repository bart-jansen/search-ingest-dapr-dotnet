# Dapr Search Ingest
End-to-end search ingestion pipeline that takes PDF documents from a folder in blob, uses Form Recognizer to extract the contents, creates embeddings & enrichments and ultimately pushes these documents to an Azure Search index.

## Overview

![Dapr overview](dapr-search-overview.drawio.svg)

With the following Dapr applications:

| Stage                | Input                               | Process                                                                                                                                                  | Output                                                                                                                             |
| -------------------- | ----------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| Batcher              | HTTP trigger with ingestion Payload | Gets list of blobs from Blob Storage                                                                                                                     | Triggers `Process Document` with reference to blob                                                                                 |
| Process Document     | Extract Document                    | Extracts contents from PDF using *Form Recognizer* and splits the content into multiple chunks/items. Contents of each item are stored in Redis Cache    | Triggers `Generate Embeddings`, `Generate Keyphrases` and `Generate Summary` for each of the split items                           |
| Generate Embeddings  | Process Document                    | Pulls the item content from Redis Cache and generates a vector representation using an OpenAI Embeddings Model. This embedding is stored in Redis Cache. | Triggers `Enrichment completed`                                                                                                    |
| Generate Keyphrases  | Process Document                    | Pulls the item content from Redis Cache and generates keyphrases using Azure Language AI API, which is stored in Redis Cache.                            | Triggers `Enrichment completed`                                                                                                    |
| Generate Summary     | Process Document                    | Pulls the item content from Redis Cache and generates summaries using Azure Language AI API, which is stored in Redis Cache.                             | Triggers `Enrichment completed`                                                                                                    |
| Enrichment completed | Generate Enrichments                | Once all enrichments are in for an item, this SearchIndexItem is stored in Blob Storage                                                                  | Once all items/sections/chunks for a single asset/document are completed, `Document completed` is triggered                        |
| Document completed   | Enrichments completed               | Once all documents are completed (tracked in Redis Cache), an Azure Search Data source/Index/Indexer are created and the indexer is invoked.             | When the indexing is done, all in-memory cache in Redis is flushed all Documents and SearchIndexItems in Blob Storage are deleted. |



## Running the project locally

**Requirements**:
- Docker
- Dotnet Core 
- Dapr

**Infra requirements (manually setup):**
- Azure Storage Account
- Azure Document Intelligence
- Azure Text Analytics
- Azure AI Search

> Deploy the resource above and populate `secrets.json`, use `secrets.example.json` as a starting point.

**Instructions**:
1. Install all services, go into each `./src/service-name/` and run `dotnet restore` and `dotnet build`
1. Run `dapr init` to initialize Dapr
1. Run `dapr run -f .` to start the services