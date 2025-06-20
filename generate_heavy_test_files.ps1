# PowerShell script to generate 20 heavy test files (2MB+ each)
param(
    [int]$FileCount = 20,
    [int]$FileSizeMB = 2,
    [string]$OutputDir = "heavy_test_files"
)

# Create output directory
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "Created directory: $OutputDir"
}

# Calculate target size in bytes (2MB = 2,097,152 bytes)
$targetSizeBytes = $FileSizeMB * 1024 * 1024
Write-Host "Target file size: $targetSizeBytes bytes ($FileSizeMB MB)"

# Base content template for realistic document content
$baseContent = @"
# Heavy Test Document for R2R Ingestion Testing

## Document Overview
This is a comprehensive test document designed to evaluate the R2R (Retrieval-Augmented Retrieval) ingestion system's performance under heavy load conditions. The document contains substantial textual content to simulate real-world document processing scenarios.

## Technical Specifications
- File Size: Approximately $FileSizeMB MB
- Content Type: Markdown text
- Purpose: R2R ingestion rate limiting and queue management testing
- Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Content Sections

### Section 1: Introduction to Large Language Models
Large Language Models (LLMs) represent a significant breakthrough in artificial intelligence and natural language processing. These sophisticated neural networks, trained on vast corpora of text data, demonstrate remarkable capabilities in understanding, generating, and manipulating human language. The development of LLMs has revolutionized numerous applications, from chatbots and virtual assistants to content generation and code completion tools.

The architecture of modern LLMs is primarily based on the Transformer model, introduced by Vaswani et al. in their seminal paper "Attention Is All You Need." This architecture leverages self-attention mechanisms to process sequences of tokens in parallel, enabling more efficient training and inference compared to previous recurrent neural network approaches.

### Section 2: Training Methodologies and Data Requirements
Training large language models requires enormous computational resources and carefully curated datasets. The process typically involves several stages: pre-training on large-scale text corpora, fine-tuning on specific tasks, and often reinforcement learning from human feedback (RLHF) to align the model's outputs with human preferences and values.

Pre-training datasets often contain hundreds of billions or even trillions of tokens, sourced from diverse text sources including web pages, books, academic papers, and other publicly available text. The quality and diversity of this training data significantly impact the model's performance and capabilities.

### Section 3: Applications and Use Cases
LLMs have found applications across numerous domains and industries. In software development, they assist with code generation, debugging, and documentation. In content creation, they help with writing, editing, and ideation. In education, they serve as tutoring assistants and learning companions. In business, they automate customer service, generate reports, and assist with data analysis.

The versatility of LLMs stems from their ability to perform few-shot and zero-shot learning, where they can adapt to new tasks with minimal or no task-specific training examples. This capability makes them particularly valuable for organizations looking to deploy AI solutions quickly and efficiently.

### Section 4: Challenges and Limitations
Despite their impressive capabilities, LLMs face several significant challenges. These include computational requirements for training and inference, potential biases inherited from training data, hallucination of false information, and difficulties in maintaining factual accuracy. Additionally, the environmental impact of training and running large models has become a growing concern.

Researchers and practitioners are actively working on addressing these challenges through various approaches, including more efficient architectures, better training techniques, improved evaluation methods, and responsible AI practices.

"@

# Generate files
for ($i = 1; $i -le $FileCount; $i++) {
    $fileName = "heavy_test_document_$($i.ToString('D2')).md"
    $filePath = Join-Path $OutputDir $fileName
    
    Write-Host "Generating file $i/$FileCount : $fileName"
    
    # Start with base content
    $content = $baseContent -replace "FILE_NUMBER", $i
    
    # Add repetitive content to reach target size
    $currentSize = [System.Text.Encoding]::UTF8.GetByteCount($content)
    
    # Add padding content to reach target size
    $paddingText = @"

### Extended Content Section $i
This section contains additional content to ensure the file reaches the target size of $FileSizeMB MB. The content is designed to be meaningful and varied to provide realistic testing conditions for the R2R ingestion system.

"@
    
    # Repeat padding until we reach target size
    $iteration = 1
    while ($currentSize -lt $targetSizeBytes) {
        $additionalContent = @"

#### Subsection ${i}.${iteration}: Advanced Topics in AI and Machine Learning
Artificial Intelligence continues to evolve rapidly, with new breakthroughs emerging regularly. Machine learning algorithms have become increasingly sophisticated, enabling applications that were previously thought impossible. Deep learning, in particular, has shown remarkable success in computer vision, natural language processing, and reinforcement learning tasks.

The field of AI research encompasses numerous subdomains, including but not limited to: neural networks, computer vision, natural language processing, robotics, expert systems, machine learning, deep learning, reinforcement learning, and artificial general intelligence. Each of these areas contributes to the overall advancement of AI technology.

Recent developments in transformer architectures have led to significant improvements in language model performance. Attention mechanisms allow models to focus on relevant parts of the input sequence, leading to better understanding and generation of text. Multi-head attention enables the model to attend to different aspects of the input simultaneously.

The training process for large language models involves several key components: tokenization of input text, embedding layers to convert tokens to vectors, multiple transformer blocks with self-attention and feed-forward layers, and output layers for prediction. The optimization process uses gradient descent with various techniques like learning rate scheduling and gradient clipping.

Evaluation of language models requires comprehensive metrics beyond simple accuracy. Researchers use perplexity, BLEU scores, ROUGE scores, and human evaluation to assess model performance. Additionally, specialized benchmarks like GLUE, SuperGLUE, and others provide standardized evaluation frameworks.

The ethical implications of AI development cannot be overlooked. Issues such as bias, fairness, transparency, and accountability are crucial considerations in AI system design and deployment. Responsible AI practices include diverse training data, bias detection and mitigation, explainable AI techniques, and ongoing monitoring of deployed systems.

"@
        
        $content += $additionalContent
        $currentSize = [System.Text.Encoding]::UTF8.GetByteCount($content)
        $iteration++
        
        # Safety check to prevent infinite loop
        if ($iteration -gt 1000) {
            Write-Warning "Reached maximum iterations for file $fileName"
            break
        }
    }
    
    # Write content to file
    $content | Out-File -FilePath $filePath -Encoding UTF8
    
    # Verify file size
    $actualSize = (Get-Item $filePath).Length
    $actualSizeMB = [math]::Round($actualSize / 1MB, 2)
    
    Write-Host "  Created: $fileName ($actualSize bytes = $actualSizeMB MB)"
    
    if ($actualSize -lt $targetSizeBytes) {
        Write-Warning "  File $fileName is smaller than target size!"
    }
}

Write-Host "`nFile generation completed!"
Write-Host "Generated $FileCount files in directory: $OutputDir"

# Display summary
$totalSize = (Get-ChildItem $OutputDir -File | Measure-Object -Property Length -Sum).Sum
$totalSizeMB = [math]::Round($totalSize / 1MB, 2)
Write-Host "Total size: $totalSize bytes ($totalSizeMB MB)"
Write-Host "Average file size: $([math]::Round($totalSize / $FileCount / 1MB, 2)) MB"
