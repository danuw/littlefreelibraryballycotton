import React, { useState } from 'react';

const ImageUpload: React.FC = () => {
    const [file, setFile] = useState<File | null>(null);
    const [uploading, setUploading] = useState<boolean>(false);
    const [uploadStatus, setUploadStatus] = useState<string>("");
    const [previewURL, setPreviewURL] = useState<string | null>(null);

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const files = e.target.files;
        if (files && files.length > 0) {
            const currentFile = files[0];
            setFile(currentFile);

            const objectURL = URL.createObjectURL(currentFile);
            setPreviewURL(objectURL);
        } else {
            setPreviewURL(null);
        }
    };

    const handleUpload = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        if (!file) {
            setUploadStatus("Please select a file before uploading.");
            return;
        }

        const formData = new FormData();
        formData.append('fileToUpload', file);

        try {
            setUploading(true);

            const response = await fetch('/api/FileUpload2', {
                method: 'POST',
                body: formData
            });

            const data = await response.json();

            if (response.ok) {
                setUploadStatus("File uploaded successfully.");
            } else {
                setUploadStatus(`Upload failed: ${data.message || "Unknown error"}`);
            }
        } catch (error: any) {
            setUploadStatus(`Upload error: ${error.message || "An unknown error occurred"}`);
        } finally {
            setUploading(false);
        }
    };

    return (
        <div>
            <h2>Upload a photo of the latest content of the little library</h2>
            <form onSubmit={handleUpload}>
                <label htmlFor="fileInput">Choose an image:</label>
                <input type="file" id="fileInput" name="fileToUpload" accept="image/*" onChange={handleFileChange} required />
                <br /><br />
                {previewURL && <img src={previewURL} alt="Selected Preview" style={{ width: '200px', marginTop: '10px' }} />}
                <br /><br />
                <button type="submit" disabled={uploading}>{uploading ? 'Uploading...' : 'Upload Image'}</button>
            </form>
            {uploadStatus && <p>{uploadStatus}</p>}
        </div>
    );
};

export default ImageUpload;
