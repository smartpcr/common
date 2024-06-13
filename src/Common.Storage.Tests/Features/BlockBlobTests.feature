Feature: BlockBlobTests
	validate block blob operations

@blockblob @create @integration_test
Scenario: create block blob
	Given a new blob with uri "https://test.blob.core.windows.net/testcontainer/testblob"
    When I create blob with content "test content"
    Then I should be able to fetch blob attributes
	And the block type should be "BlockBlob"
	And I should be able to download blob
	And the downloaded content should be "test content"
	And the blob uri should be "http://127.0.0.1:10000/devstoreaccount1/testcontainer/testblob"

@pageblob @create @integration_test
	Scenario: create page blobs
	    Given the following page blobs and their size
	     | BlobUri                                                                                           | Size |
         | https://crptestcollateral.blob.core.windows.net/vhds/2012R2VHD.vhd                                | 30G  |
         | https://crptestcollateral.blob.core.windows.net/vhds/4MPageBlob.vhd                               | 4M   |
         | https://crptestcollateral.blob.core.windows.net/vhds/folder1/folder2/4MPageBlobWithFolderPath.vhd | 4M   |
       When I ensure page blobs are created
       Then I should be able to fetch attributes of page blobs
       And page blobs should have the following properties
        | ContainerName | BlobName       | Size        | BlobType |
        | vhds          | 2012R2VHD.vhd  | 32212254720 | PageBlob |
        | vhds          | 4MPageBlob.vhd | 4194304     | PageBlob |
        | vhds          | folder1/folder2/4MPageBlobWithFolderPath.vhd | 4194304 | PageBlob |
