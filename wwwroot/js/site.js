// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Function to refresh comments preview in feed
function refreshCommentsPreview(postId) {
    // Find the post card
    const postCard = document.getElementById(`post-${postId}`);
    
    if (!postCard) {
        return;
    }
    
    // Check if this post has a card-footer (indicates it's on feed/dashboard)
    const cardFooter = postCard.querySelector('.card-footer');
    if (!cardFooter) {
        return;
    }
    
    // Fetch updated comments preview
    fetch(`/Posts/GetCommentsPreview?postId=${postId}`, {
        method: 'GET',
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.text();
    })
    .then(html => {
        let previewContainer = document.getElementById(`comments-preview-${postId}`);
        
        if (html.trim()) {
            // There are comments to display
            if (previewContainer) {
                // Update existing container
                previewContainer.innerHTML = html;
            } else {
                // Create new container
                const cardBody = postCard.querySelector('.card-body');
                if (cardBody) {
                    const div = document.createElement('div');
                    div.className = 'mt-3 pt-2 border-top';
                    div.id = `comments-preview-${postId}`;
                    div.innerHTML = html;
                    cardBody.appendChild(div);
                }
            }
        } else {
            // No comments - remove preview if it exists
            if (previewContainer) {
                previewContainer.remove();
            }
        }
    })
    .catch(error => {
        console.error('Error refreshing comments preview:', error);
    });
}

// ...existing code...
document.addEventListener('DOMContentLoaded', () => {
    (function(){
        const chatModal = document.getElementById('groupChatModal');
        if (!chatModal) return;

        const toggle = document.getElementById('groupInfoToggle');
        const panel = document.getElementById('groupInfoPanel');
        const content = chatModal.querySelector('.modal-content');
        const closeBtn = document.getElementById('groupInfoClose');

        if (!toggle || !panel || !content || !closeBtn) {
            console.error('Group info panel elements not found.');
            return;
        }

        async function openPanel() {
            panel.classList.add('open');
            content.classList.add('shifted');
            
            const title = document.getElementById('chatModalTitle')?.textContent?.trim() || 'Group';
            document.getElementById('panelGroupName').textContent = title;
            
            const list = document.getElementById('panelMembersList');
            list.innerHTML = '<div class="text-muted small p-3">Loading...</div>';
            
            const gid = document.getElementById('currentGroupId')?.value;
            if (!gid) {
               list.innerHTML = '<div class="text-muted small p-3">Members not available</div>';
               return;
            }

            try {
                const groupId_forLeaveGroupForm = document.getElementById('groupId_forLeaveGroupForm');
                if (groupId_forLeaveGroupForm) {
                    groupId_forLeaveGroupForm.value = gid;
                }
                // First, check if the current user is authorized as a moderator/admin
                const authResponse = await fetch(`/Group/IsAuthorizedInGroup?groupId=${encodeURIComponent(gid)}`);
                if (!authResponse.ok) throw new Error('Auth check failed');
                const authData = await authResponse.json();
                const isAuthorized = authData.groupRole === 'Moderator';

                // Second, fetch the member list
                const membersResponse = await fetch(`/Group/GetGroupMembers?groupId=${encodeURIComponent(gid)}`);
                if (!membersResponse.ok) throw new Error('Member fetch failed');
                const members = await membersResponse.json();

                list.innerHTML = ''; // Clear loading indicator
                document.getElementById('panelGroupCount').textContent = `${members.length} member${members.length !== 1 ? 's' : ''}`;
                
                const searchbar = document.getElementById("GroupInfoAddMembers");
                const searchbar_submitbutton = document.getElementById("addMembersBtn_panel")
                // RENDER MODERATOR SETTINGS FORM (if authorized) 
                if (isAuthorized) {
                    searchbar.display = 'block';
                    searchbar_submitbutton.style.display = 'block';
                    searchbar.style.display = 'block';
                    searchbar_submitbutton.style.display = 'block';
                    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
                    const formHtml = `
                        <form id="groupSettingsForm" action="/Group/UpdateSettings" method="post" enctype="multipart/form-data">
                            <input type="hidden" name="groupId" value="${gid}" />
                            <input type="hidden" name="__RequestVerificationToken" value="${token}" />
                            
                            <div class="mb-3">
                                <label for="groupNameInput" class="form-label fw-bold small">Group Name</label>
                                <input type="text" class="form-control" id="groupNameInput" name="groupName" value="${title}">
                            </div>

                            <div class="mb-4">
                                <label for="groupPfpInput" class="form-label fw-bold small">Change Group Picture</label>
                                <input class="form-control form-control-sm" type="file" id="groupPfpInput" name="groupPfpFile" accept="image/*">
                            </div>

                            <h6 class="fw-bold small mb-2">Manage Moderators</h6>
                            <div id="moderatorMemberList"></div>

                            <button type="submit" class="btn btn-primary w-100 mt-3">Save Changes</button>
                        </form>
                    `;
                    list.innerHTML = formHtml;
                    
                    
                    const memberListContainer = list.querySelector('#moderatorMemberList');
                    if (!memberListContainer) {
                        console.error('moderatorMemberList container not found after form insertion');
                        return;
                    }

                    members.forEach(m => {
                        const el = document.createElement('div');
                        el.className = 'd-flex align-items-center justify-content-between mb-2 p-2 rounded bg-light';
                        
                        const canToggle = m.userId !== authData.currentUserId;
                        const isCurrentlyModerator = m.role === 'Moderator';
                        
                        const leftHtml = `
                            <div class="d-flex align-items-center">
                                <img src="${m.pfpURL || '/uploads/default_pfp.jpg'}" alt="${m.userName}" class="rounded-circle me-2" width="34" height="34" style="object-fit:cover;">
                                <div>
                                    <div class="fw-semibold small">${m.userName}</div>
                                    <div class="small text-muted">${m.role || 'Member'}</div>
                                </div>
                            </div>
                        `;

                        const checkboxHtml = canToggle
                            ? `<div class="form-check form-switch">
                                    <input class="form-check-input" type="checkbox" role="switch" name="moderatorIds" value="${m.userId}" id="mod_${m.userId}" ${isCurrentlyModerator ? 'checked' : ''}>
                                    <label class="form-check-label small" for="mod_${m.userId}">Mod</label>
                               </div>`
                            : `<div class="text-muted small"><em>Owner/Self</em></div>`;
                        const kick = canToggle
                        ? `<button type="button" class="btn btn-sm btn-outline-danger ms-3 kick-member-btn" data-user-id="${m.userId}" data-group-id="${gid}">Kick</button>`
                        : '';
                        // const kickGID= document.getElementById('groupId_forKickForm');
                        // const kickUID= document.getElementById('userId_forKickForm');
                        // kickGID.value = gid;
                        // kickUID.value = m.userId;

                        el.innerHTML = leftHtml + checkboxHtml + kick;
                        memberListContainer.appendChild(el);
                    });

                } else {
                    // --- RENDER SIMPLE MEMBER LIST (for non-moderators) ---
                    searchbar.display = 'none';
                    searchbar_submitbutton.display = 'none';
                    searchbar.style.display = 'none';
                    searchbar_submitbutton.style.display = 'none';
                    if (!members || !members.length) {
                        list.innerHTML = '<div class="text-muted small p-3">No members</div>';
                        return;
                    }
                    
                    // Create a container for members only (no button here)
                    const membersList = document.createElement('div');
                    members.forEach(m => {
                        const el = document.createElement('div');
                        el.className = 'd-flex align-items-center mb-2';
                        el.innerHTML = `<img src="${m.pfpURL || '/uploads/default_pfp.jpg'}" alt="${m.userName}" class="rounded-circle me-2" width="34" height="34" style="object-fit:cover;"><div><div class="fw-semibold small">${m.userName}</div><div class="small text-muted">${m.role || 'Member'}</div></div>`;
                        membersList.appendChild(el);
                    });

                    list.innerHTML = '';
                    list.appendChild(membersList);
                    
                }

            } catch (err) {
                list.innerHTML = '<div class="text-muted small p-3">Unable to load group details</div>';
                console.error(err);
            }
            document.addEventListener('click', function (e) {
  const btn = e.target.closest('.kick-member-btn');
  if (!btn) return;
  e.preventDefault();
  const userId = btn.dataset.userId;
  const groupId = btn.dataset.groupId;

  const kickForm = document.getElementById('KickForm');
  const kickGID = document.getElementById('groupId_forKickForm');
  const kickUID = document.getElementById('userId_forKickForm');

  if (kickGID) kickGID.value = groupId;
  if (kickUID) kickUID.value = userId;

  // Submit the existing form
  if (kickForm) kickForm.submit();
});
        }

        function closePanel() {
            panel.classList.remove('open');
            content.classList.remove('shifted');
        }

        toggle.addEventListener('click', (e) => {
            e.preventDefault();
            if (panel.classList.contains('open')) {
                closePanel();
            } else {
                openPanel();
            }
        });

        closeBtn.addEventListener('click', (e) => {
            e.preventDefault();
            closePanel();
        });

        // Close panel when modal is hidden
        chatModal.addEventListener('hidden.bs.modal', closePanel);

    })();


    const input = document.querySelector('#searchInput');
    const results = document.querySelector('#searchResults');
    if (!input || !results) return;

    input.addEventListener('input', async () => {
        const q = input.value.trim();
        if (!q) { results.innerHTML = ''; results.style.display = 'none'; return; }

        try {
            const res = await fetch(`/Search/Live?q=${encodeURIComponent(q)}`);
            if (!res.ok) {
                const text = await res.text();
                console.error('Search error', res.status, text);
                results.innerHTML = '';
                results.style.display = 'none';
                return;
            }
            results.innerHTML = await res.text();
            results.style.display = 'block';
        } catch (err) {
            console.error('Search fetch failed:', err);
            results.innerHTML = '';
            results.style.display = 'none';
        }
    });

    const input_chatSideBar = document.querySelector('#searchInput_chat');
    const results_chatSideBar = document.querySelector('#searchResults_chat');
    const selectedMembersContainer = document.querySelector('#selectedMembers');
    
    // ADD NULL CHECK HERE (CRITICAL FIX)
    if (input_chatSideBar && results_chatSideBar) {
        input_chatSideBar.addEventListener('input', async () => {
            const q = input_chatSideBar.value.trim();
            if (!q) { results_chatSideBar.innerHTML = ''; results_chatSideBar.style.display = 'none'; return; }

            try {
                const res = await fetch(`/Search/LiveChat?q=${encodeURIComponent(q)}`);
                if (!res.ok) {
                    const text = await res.text();
                    console.error('Search error', res.status, text);
                    results_chatSideBar.innerHTML = '';
                    results_chatSideBar.style.display = 'none';
                    return;
                }
                results_chatSideBar.innerHTML = await res.text();
                results_chatSideBar.style.display = 'block';
            } catch (err) {
                console.error('Search fetch failed:', err);
                results_chatSideBar.innerHTML = '';
                results_chatSideBar.style.display = 'none';
            }
        });
    }
    
    const selectedUsers = new Set();

    if (results_chatSideBar && selectedMembersContainer) {
        results_chatSideBar.addEventListener('click', (e) => {
            const listItem = e.target.closest('li');
            if (!listItem) return;

            // Get data from attributes we added in the partial view
            const userId = listItem.dataset.userId;
            const username = listItem.dataset.username;
            const pfpSrc = listItem.dataset.pfp;

            if (userId && !selectedUsers.has(userId)) {
                selectedUsers.add(userId);

                // 1. Create the Visual "Chip"
                const chip = document.createElement('div');
                chip.className = 'user-chip animate-pop';
                chip.innerHTML = `
                    <img src="${pfpSrc}" alt="" />
                    <span>${username}</span>
                    <button type="button" class="btn-close btn-close-white ms-2" aria-label="Remove" style="width: 0.5em; height: 0.5em;"></button>
                    <!-- 2. Create Hidden Input for Form Submission -->
                    <input type="hidden" name="SelectedUserIds" value="${userId}" />
                `;

                selectedMembersContainer.appendChild(chip);

                // 3. Clear search to allow adding next user easily
                input_chatSideBar.value = '';
                results_chatSideBar.style.display = 'none';
                input_chatSideBar.focus();

                // 4. Remove Logic
                chip.querySelector('.btn-close').addEventListener('click', () => {
                    chip.remove(); // Removes the visual + the hidden input inside it
                    selectedUsers.delete(userId);
                });
            }
            else{
                // User already selected, just clear input and results
                input_chatSideBar.value = '';
                results_chatSideBar.style.display = 'none';
                input_chatSideBar.focus();   
            }
        });
    }

    // hide results when clicking outside
    document.addEventListener('click', (e) => {
        if (e.target !== input && !results.contains(e.target)) {
            results.style.display = 'none';
        }
    });

    // =================================================================
    // Centralized Event Handlers for Likes, Comments, etc.
    // =================================================================

    // Use event delegation on the document to handle clicks on dynamic content
    document.addEventListener('click', function (e) {
        const likeButton = e.target.closest('.like-button');
        const editButton = e.target.closest('.edit-comment-btn');
        const deleteButton = e.target.closest('.delete-comment-btn');

        // --- Handle Like Button Click ---
        if (likeButton) {
            e.preventDefault();
            const postId = likeButton.dataset.postId;
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            fetch('/Posts/ToggleLike', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: `postId=${postId}&__RequestVerificationToken=${encodeURIComponent(token)}`
            }).then(response => response.json()).then(data => {
                if (data.success) {
                    document.querySelectorAll(`.like-button[data-post-id='${postId}']`).forEach(btn => {
                        const icon = btn.querySelector('i');
                        if (data.liked) {
                            icon.classList.remove('bi-heart');
                            icon.classList.add('bi-heart-fill', 'text-danger');
                        } else {
                            icon.classList.remove('bi-heart-fill', 'text-danger');
                            icon.classList.add('bi-heart');
                        }
                    });
                    
                    // Update likes count in all locations (profile grid, modal, etc.)
                    document.querySelectorAll(`#post-${postId} .likes-count, #post-modal-${postId} .likes-count`).forEach(span => {
                        // Check if it's a badge (on profile grid) or regular span (in modal)
                        if (span.classList.contains('badge')) {
                            // For badge in profile grid, keep the icon
                            span.innerHTML = `<i class="bi bi-heart-fill text-danger"></i> ${data.likesCount}`;
                        } else {
                            // For modal, just show number + "likes"
                            span.textContent = `${data.likesCount} likes`;
                        }
                    });
                }
            }).catch(error => console.error('Error toggling like:', error));
        }

        // --- Handle Edit Button Click (to show the form) ---
        if (editButton) {
            e.preventDefault();
            const commentId = editButton.dataset.commentId;
            
            // Determine if we are inside a modal
            const inPostModal = editButton.closest('#postModal');
            const inCommentsModal = editButton.closest('#commentsModal');

            let displaySelector, formSelector;

            if (inPostModal || inCommentsModal) {
                // If in any modal, target elements within that modal context
                const modalContext = inPostModal || inCommentsModal;
                displaySelector = `#${modalContext.id} #comment-display-${commentId}`;
                formSelector = `#${modalContext.id} .edit-comment-form[data-comment-id='${commentId}']`;
            } else {
                // If on the main page (feed preview), target elements without modal context
                // This assumes feed comments are not inside a modal structure, which is correct.
                displaySelector = `#comments-preview-container #comment-display-${commentId}`;
                formSelector = `#comments-preview-container .edit-comment-form[data-comment-id='${commentId}']`;
            }

            const displayDiv = document.querySelector(displaySelector);
            const editForm = document.querySelector(formSelector);

            if (displayDiv && editForm) {
                displayDiv.style.display = 'none';
                editForm.style.display = 'block';
                editForm.querySelector('input[name="content"]').focus();
            } else {
                // Fallback for any case not covered, like comments page
                const genericDisplay = document.getElementById(`comment-display-${commentId}`);
                const genericForm = document.querySelector(`.edit-comment-form[data-comment-id='${commentId}']`);
                if(genericDisplay && genericForm) {
                    genericDisplay.style.display = 'none';
                    genericForm.style.display = 'block';
                    genericForm.querySelector('input[name="content"]').focus();
                }
            }
        }

        // --- Handle Delete Button Click ---
        if (deleteButton) {
            e.preventDefault();
            const commentId = deleteButton.dataset.commentId;
            
            if (confirm('Are you sure you want to delete this comment?')) {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                
                // Get the container before deletion to find the postId
                const container = document.getElementById(`comment-container-${commentId}`);
                let postId = null;
                
                // Try to find the postId from the container's parent structure
                if (container) {
                    // Look for the post ID in the comments container (modal)
                    const commentsContainer = container.closest('[id^="comments-list-"], [id^="comments-for-modal-"]');
                    if (commentsContainer) {
                        const match = commentsContainer.id.match(/comments-(list|for-modal)-(\d+)/);
                        if (match) {
                            postId = match[2];
                        }
                    }
                    
                    // If not found, try to find the post card in feed
                    if (!postId) {
                        const postCard = container.closest('[id^="post-"]');
                        if (postCard) {
                            const match = postCard.id.match(/post-(\d+)/);
                            if (match) {
                                postId = match[1];
                            }
                        }
                    }
                    
                    // If still not found, check if we're in the comments modal
                    if (!postId) {
                        const commentsModal = document.getElementById('commentsModal');
                        if (commentsModal && commentsModal.classList.contains('active')) {
                            const modalPostIdInput = document.getElementById('modalPostId');
                            if (modalPostIdInput) {
                                postId = modalPostIdInput.value;
                            }
                        }
                    }
                    
                    // If still not found, check if we're in the post modal
                    if (!postId) {
                        const postModalBody = container.closest('#postModalBody');
                        if (postModalBody) {
                            // Try to find post ID from any element with data-post-id in the modal
                            const postElement = postModalBody.querySelector('[data-post-id]');
                            if (postElement) {
                                postId = postElement.dataset.postId;
                            }
                        }
                    }
                }
                
                fetch(`/Comments/Delete/${commentId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: `__RequestVerificationToken=${encodeURIComponent(token)}`
                })
                .then(async response => { // Make this async to await the body text
                    if (!response.ok) {
                        // Try to get a JSON error message from the body
                        const errorData = await response.json().catch(() => null);
                        const errorMessage = errorData?.message || `Network response was not ok (Status: ${response.status})`;
                        throw new Error(errorMessage);
                    }
                    return response.json();
                })
                .then(data => {
                    if (data.success) {
                        // Remove the same comment from all locations (feed preview, modal, comments modal)
                        const allCommentContainers = document.querySelectorAll(`#comment-container-${commentId}`);
                        allCommentContainers.forEach(c => {
                            c.style.transition = 'opacity 0.3s';
                            c.style.opacity = '0';
                            setTimeout(() => c.remove(), 300);
                        });
                        
                        // Update comment count for this post (after a delay to ensure container is identified)
                        // Use a flag to ensure we only update once per post
                        const updatedPosts = new Set();
                        
                        setTimeout(() => {
                            if (postId && !updatedPosts.has(postId)) {
                                updatedPosts.add(postId);
                                
                                // Update all comment counts for this post across the entire page
                                const allCommentCounts = document.querySelectorAll('.comments-count');
                                allCommentCounts.forEach(span => {
                                    // Check if this counter belongs to our post by finding the parent with the post ID
                                    const postContainer = span.closest(`#post-${postId}`) || 
                                                        span.closest(`#post-modal-${postId}`) ||
                                                        span.closest(`[id="post-${postId}"]`) ||
                                                        span.closest(`[data-post-id="${postId}"]`);
                                    
                                    if (postContainer) {
                                        // Extract current count from text (handles both "X comments" and badge with icon)
                                        const textContent = span.textContent.trim();
                                        const matches = textContent.match(/\d+/);
                                        const currentCount = matches ? parseInt(matches[0]) : 0;
                                        const newCount = Math.max(0, currentCount - 1);
                                        
                                        // Check if this is a badge (profile grid) or regular text
                                        if (span.classList.contains('badge')) {
                                            // Badge format - just number with icon
                                            span.innerHTML = `<i class="bi bi-chat-fill"></i> ${newCount}`;
                                        } else {
                                            // Regular format - "X comments"
                                            span.textContent = `${newCount} comment${newCount !== 1 ? 's' : ''}`;
                                        }
                                    }
                                });
                                
                                // Also update the comments modal counter if it's open and showing this post
                                const commentsModal = document.getElementById('commentsModal');
                                if (commentsModal && commentsModal.classList.contains('active')) {
                                    const modalPostIdInput = document.getElementById('modalPostId');
                                    if (modalPostIdInput && modalPostIdInput.value === postId) {
                                        const modalCounter = commentsModal.querySelector('.comments-count');
                                        if (modalCounter) {
                                            const currentCount = parseInt(modalCounter.textContent) || 0;
                                            modalCounter.textContent = Math.max(0, currentCount - 1);
                                        }
                                    }
                                }
                                
                                // Also check for modal-specific counters
                                const modalCommentsCount = document.querySelector(`#post-modal-${postId} .modal-comments-count`);
                                if (modalCommentsCount) {
                                    const currentCount = parseInt(modalCommentsCount.textContent) || 0;
                                    const newCount = Math.max(0, currentCount - 1);
                                    modalCommentsCount.textContent = newCount;
                                }
                                
                                // Check if we're in the comments modal and if it's now empty
                                const commentsModalBody = document.getElementById('commentsModalBody');
                                if (commentsModalBody && commentsModalBody.children.length === 0) {
                                    commentsModalBody.innerHTML = '<div class="no-comments"><i class="bi bi-chat-dots"></i><p>No comments yet. Be the first to comment!</p></div>';
                                }
                                
                                // Reload comments preview in feed
                                refreshCommentsPreview(postId);
                            }
                        }, 350);
                    } else {
                        alert(data.message || 'Failed to delete comment.');
                    }
                }).catch(err => {
                    console.error('Delete error:', err);
                    alert(err.message); // Display the more specific error message
                });
            }
        }
    });

    // Use event delegation for form submissions
    document.addEventListener('submit', function (e) {
        // --- Handle Add Comment Form Submission ---
        if (e.target.matches('.add-comment-form') && e.target.id !== 'modalCommentForm') {
            e.preventDefault();
            const form = e.target;
            
            // Get postId from either data-post-id attribute or hidden input (for modal)
            let postId = form.dataset.postId || form.getAttribute('data-post-id');
            
            // If not found, check for the hidden input in the modal form
            if (!postId) {
                const hiddenInput = form.querySelector('#modalPostId');
                if (hiddenInput) {
                    postId = hiddenInput.value;
                }
            }
            
            const input = form.querySelector('input[name="commentText"]');
            const content = input.value;

            if (!content.trim()) return;

            fetch('/Comments/Add', {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/x-www-form-urlencoded', 
                    'X-Requested-With': 'XMLHttpRequest' 
                },
                body: `postId=${postId}&content=${encodeURIComponent(content)}`
            })
            .then(async response => {
                const text = await response.text();
                
                // Check if response is an error (starts with error indicator or is empty)
                if (!response.ok || text.includes('alert-danger') || text.includes('error')) {
                    // Try to parse JSON error message
                    try {
                        const errorData = JSON.parse(text);
                        throw new Error(errorData.message || 'Failed to add comment');
                    } catch (e) {
                        // If not JSON, check if HTML contains error message
                        const tempDiv = document.createElement('div');
                        tempDiv.innerHTML = text;
                        const errorMsg = tempDiv.querySelector('.alert-danger')?.textContent.trim();
                        throw new Error(errorMsg || 'Failed to add comment due to content validation');
                    }
                }
                
                return text;
            })
            .then(html => {
                const commentsContainer = 
                    document.getElementById(`comments-list-${postId}`) ||
                    document.getElementById(`comments-for-modal-${postId}`);
                
                if (commentsContainer) {
                    commentsContainer.insertAdjacentHTML('beforeend', html);
                }
                input.value = '';
                
                // Update comment count - search for all comment counts for this post
                const allCommentCounts = document.querySelectorAll('.comments-count');
                allCommentCounts.forEach(span => {
                    const postContainer = span.closest(`#post-${postId}`) || 
                                        span.closest(`#post-modal-${postId}`) ||
                                        span.closest(`[id="post-${postId}"]`) ||
                                        span.closest(`[data-post-id="${postId}"]`);
                    
                    if (postContainer) {
                        // Extract current count from text (handles both "X comments" and badge with icon)
                        const textContent = span.textContent.trim();
                        const matches = textContent.match(/\d+/);
                        const currentCount = matches ? parseInt(matches[0]) : 0;
                        const newCount = currentCount + 1;
                        
                        // Check if this is a badge (profile grid) or regular text
                        if (span.classList.contains('badge')) {
                            // Badge format - just number with icon
                            span.innerHTML = `<i class="bi bi-chat-fill"></i> ${newCount}`;
                        } else {
                            // Regular format - "X comments"
                            span.textContent = `${newCount} comment${newCount !== 1 ? 's' : ''}`;
                        }
                    }
                });
                
                // Reload comments preview in feed (with slight delay to ensure comment is added)
                setTimeout(() => {
                    refreshCommentsPreview(postId);
                }, 100);
            })
            .catch(error => {
                console.error('Error adding comment:', error);
                alert(error.message || 'Failed to add comment. Please check your content.');
            });
        }

        // --- Handle Edit Comment Form Submission ---
        if (e.target.matches('.edit-comment-form')) {
            e.preventDefault();
            const form = e.target;
            const commentId = form.dataset.commentId;
            const content = form.querySelector('input[name="content"]').value;
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            fetch(`/Comments/Edit/${commentId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: `content=${encodeURIComponent(content)}&__RequestVerificationToken=${encodeURIComponent(token)}`
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Update ALL instances of this comment across the page (preview, modals, etc.)
                    const allContentSpans = document.querySelectorAll(`#comment-content-${commentId}`);
                    const allDisplayDivs = document.querySelectorAll(`#comment-display-${commentId}`);
                    const allEditForms = document.querySelectorAll(`.edit-comment-form[data-comment-id="${commentId}"]`);
                    
                    allContentSpans.forEach(span => {
                        span.textContent = data.content;
                    });
                    
                    allDisplayDivs.forEach(div => {
                        div.style.display = 'flex';
                    });
                    
                    allEditForms.forEach(f => {
                        f.style.display = 'none';
                    });
                } else { 
                    alert(data.message || 'Failed to edit comment.'); 
                }
            }).catch(err => {
                console.error('Edit error:', err);
                alert('An error occurred while editing the comment.');
            });
        }
    });

    // =================================================================
    // Video Autoplay with Intersection Observer
    // =================================================================
    function initializeVideoAutoplay() {
        const videos = document.querySelectorAll('.feed-video');
        
        const observerOptions = {
            root: null,
            rootMargin: '0px',
            threshold: 0.5 // Video must be 50% visible
        };

        const videoObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                const video = entry.target;
                
                if (entry.isIntersecting) {
                    // Video is visible, play it
                    video.muted = true; // Start muted for autoplay
                    video.play().catch(err => console.log('Autoplay prevented:', err));
                } else {
                    // Video is not visible, pause it
                    video.pause();
                }
            });
        }, observerOptions);

        videos.forEach(video => {
            // Remove loop attribute and add ended event listener
            video.removeAttribute('loop');
            
            // When video ends, pause it so user can replay manually
            video.addEventListener('ended', function() {
                this.pause();
                this.currentTime = 0; // Reset to beginning
            });
            
            videoObserver.observe(video);
        });
    }

    // Initialize video autoplay
    initializeVideoAutoplay();

    // =================================================================
    // Infinite Scroll for Home Feed
    // =================================================================
    function initializeInfiniteScroll() {
        const mainContent = document.querySelector('.main-content');
        const feedContainer = document.getElementById('feed-container');
        const loadingIndicator = document.getElementById('loading');

        // Exit if the necessary elements aren't on this page
        if (!mainContent || !feedContainer || !loadingIndicator) {
            return;
        }

        let page = 1;
        let isLoading = false;
        let noMorePosts = false;

        mainContent.onscroll = () => {
            if (isLoading || noMorePosts) return;

            // Check if user is near the bottom (added a slightly larger threshold)
            if (mainContent.scrollHeight - mainContent.scrollTop <= mainContent.clientHeight + 200) {
                isLoading = true;
                loadingIndicator.innerHTML = '<div class="spinner-border text-purple" role="status"><span class="visually-hidden">Loading...</span></div>';
                loadingIndicator.style.display = 'block';

                fetch(`/Home/LoadMorePosts?page=${page}`)
                    .then(response => response.text())
                    .then(data => {
                        if (data && data.trim().length > 0) {
                            feedContainer.insertAdjacentHTML('beforeend', data);
                            page++;
                            loadingIndicator.style.display = 'none';
                            loadingIndicator.innerHTML = '';
                            // Reinitialize video autoplay for newly loaded videos
                            initializeVideoAutoplay();
                        } else {
                            // No more posts to load
                            noMorePosts = true;
                            loadingIndicator.innerHTML = '<p class="text-muted small mt-4">You have reached the end.</p>';
                            // Keep the message displayed
                        }
                        isLoading = false;
                    }).catch(() => { isLoading = false; loadingIndicator.style.display = 'none'; loadingIndicator.innerHTML = ''; });
            }
        };
    }

    initializeInfiniteScroll();

    // =================================================================
    // Post Modal Loading for Profile Page
    // =================================================================
    function initializePostModal() {
        const postModal = document.getElementById('postModal');
        if (!postModal) {
            return; // Exit if the modal isn't on this page
        }

        // Remove any existing event listeners to prevent duplicates
        postModal.removeEventListener('show.bs.modal', handleModalShow);
        postModal.addEventListener('show.bs.modal', handleModalShow);
        
        // Add event listener for when modal is fully shown
        postModal.addEventListener('shown.bs.modal', function() {
            initializeModalCommentsScroll();
            
            // Ensure all videos in modal start muted
            const videos = postModal.querySelectorAll('video');
            videos.forEach(video => {
                video.muted = true;
            });
        });
        
        // Add event listener for when modal is hidden to stop videos
        postModal.addEventListener('hidden.bs.modal', function() {
            const videos = postModal.querySelectorAll('video');
            videos.forEach(video => {
                video.pause();
                video.currentTime = 0;
            });
        });
    }

    function handleModalShow(event) {
        const button = event.relatedTarget; // The thumbnail that was clicked
        if (!button) {
            console.error('No button trigger found');
            return;
        }
        
        const postId = button.getAttribute('data-post-id');
        if (!postId) {
            console.error('No post ID found on button:', button);
            return;
        }
        
        const modalBody = document.getElementById('postModalBody');
        if (!modalBody) {
            console.error('Modal body not found');
            return;
        }

        // Show a loading spinner
        modalBody.innerHTML = '<div class="d-flex justify-content-center p-5"><div class="spinner-border text-purple" role="status"><span class="visually-hidden">Loading...</span></div></div>';

        fetch(`/Posts/PostPartial/${postId}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.text();
            })
            .then(html => { 
                modalBody.innerHTML = html;
                
                // Ensure all videos in modal start muted
                const modalVideos = modalBody.querySelectorAll('video');
                modalVideos.forEach(video => {
                    video.muted = true;
                });
                
                // Initialize scroll after content is loaded
                setTimeout(initializeModalCommentsScroll, 100);
            })
            .catch(err => { 
                console.error('Modal load error:', err);
                modalBody.innerHTML = '<p class="text-danger text-center p-5">Failed to load post. Please try again.</p>'; 
            });
    }
    
    // Initialize infinite scroll for comments in profile modal
    function initializeModalCommentsScroll() {
        const modalBody = document.getElementById('postModalBody');
        if (!modalBody) return;
        
        // Find all scroll containers with the correct ID pattern
        const scrollContainers = modalBody.querySelectorAll('[id^="modal-comments-container-"]');
        
        scrollContainers.forEach(scrollContainer => {
            const postId = scrollContainer.id.replace('modal-comments-container-', '');
            let page = 2; // Start from page 2 since we loaded first 10 already
            let isLoading = false;
            let hasMore = true;
            
            // Remove any existing scroll listeners to prevent duplicates
            if (scrollContainer._commentsScrollHandler) {
                scrollContainer.removeEventListener('scroll', scrollContainer._commentsScrollHandler);
            }
            
            scrollContainer._commentsScrollHandler = function() {
                if (isLoading || !hasMore) return;
                
                const scrollTop = scrollContainer.scrollTop;
                const scrollHeight = scrollContainer.scrollHeight;
                const clientHeight = scrollContainer.clientHeight;
                
                // Load more when scrolled near bottom
                if (scrollTop + clientHeight >= scrollHeight - 50) {
                    isLoading = true;
                    
                    // Show loading indicator
                    const loadingIndicator = scrollContainer.querySelector('.modal-comments-loading');
                    if (loadingIndicator) {
                        loadingIndicator.style.display = 'block';
                    }
                    
                    fetch(`/Comments/LoadComments?postId=${postId}&page=${page}&pageSize=10`, {
                        headers: { 'X-Requested-With': 'XMLHttpRequest' }
                    })
                    .then(response => response.text())
                    .then(html => {
                        if (html.trim().length === 0) {
                            hasMore = false;
                            if (loadingIndicator) {
                                loadingIndicator.style.display = 'none';
                            }
                        } else {
                            // Insert before the loading indicator
                            const commentsDiv = scrollContainer.querySelector(`#comments-for-modal-${postId}`);
                            if (commentsDiv && loadingIndicator) {
                                loadingIndicator.insertAdjacentHTML('beforebegin', html);
                                loadingIndicator.style.display = 'none';
                            } else if (commentsDiv) {
                                commentsDiv.insertAdjacentHTML('beforeend', html);
                            }
                            page++;
                        }
                        isLoading = false;
                    })
                    .catch(error => {
                        console.error('Error loading comments:', error);
                        if (loadingIndicator) {
                            loadingIndicator.style.display = 'none';
                        }
                        isLoading = false;
                    });
                }
            };
            
            scrollContainer.addEventListener('scroll', scrollContainer._commentsScrollHandler);
        });
    }

    // Initialize modal after a short delay to ensure Bootstrap is ready
    setTimeout(() => {
        initializePostModal();
    }, 100);
    
    // Also handle clicks on post images and videos in feed (event delegation)
    document.addEventListener('click', function(e) {
        const postImageLink = e.target.closest('.post-image-link');
        const postVideoContainer = e.target.closest('.post-video-container');
        
        // Handle video click - but not if clicking on video controls
        if (postVideoContainer && !e.target.closest('video')) {
            e.preventDefault();
            const postId = postVideoContainer.getAttribute('data-post-id');
            
            // Manually trigger the modal with the post ID
            const postModal = document.getElementById('postModal');
            if (postModal && postId) {
                const modalBody = document.getElementById('postModalBody');
                if (modalBody) {
                    modalBody.innerHTML = '<div class="d-flex justify-content-center p-5"><div class="spinner-border text-purple" role="status"><span class="visually-hidden">Loading...</span></div></div>';
                }
                
                // Pause the video in the feed
                const feedVideo = postVideoContainer.querySelector('.feed-video');
                if (feedVideo) {
                    feedVideo.pause();
                }
                
                // Show the modal
                const bsModal = new bootstrap.Modal(postModal);
                bsModal.show();
                
                // Load the content
                fetch(`/Posts/PostPartial/${postId}`)
                    .then(response => {
                        if (!response.ok) {
                            throw new Error(`HTTP error! status: ${response.status}`);
                        }
                        return response.text();
                    })
                    .then(html => { 
                        modalBody.innerHTML = html;
                        
                        // Ensure all videos in modal start muted
                        const modalVideos = modalBody.querySelectorAll('video');
                        modalVideos.forEach(video => {
                            video.muted = true;
                        });
                        
                        setTimeout(initializeModalCommentsScroll, 100);
                    })
                    .catch(err => { 
                        console.error('Modal load error:', err);
                        modalBody.innerHTML = '<p class="text-danger text-center p-5">Failed to load post. Please try again.</p>'; 
                    });
            }
            return; // Exit early to prevent image handling
        }
        
        // Handle image click
        if (postImageLink) {
            e.preventDefault();
            const postId = postImageLink.getAttribute('data-post-id');
            
            // Manually trigger the modal with the post ID
            const postModal = document.getElementById('postModal');
            if (postModal && postId) {
                const modalBody = document.getElementById('postModalBody');
                if (modalBody) {
                    modalBody.innerHTML = '<div class="d-flex justify-content-center p-5"><div class="spinner-border text-purple" role="status"><span class="visually-hidden">Loading...</span></div></div>';
                }
                
                // Show the modal
                const bsModal = new bootstrap.Modal(postModal);
                bsModal.show();
                
                // Load the content
                fetch(`/Posts/PostPartial/${postId}`)
                    .then(response => {
                        if (!response.ok) {
                            throw new Error(`HTTP error! status: ${response.status}`);
                        }
                        return response.text();
                    })
                    .then(html => { 
                        modalBody.innerHTML = html;
                        
                        // Ensure all videos in modal start muted
                        const modalVideos = modalBody.querySelectorAll('video');
                        modalVideos.forEach(video => {
                            video.muted = true;
                        });
                        
                        setTimeout(initializeModalCommentsScroll, 100);
                    })
                    .catch(err => { 
                        console.error('Modal load error:', err);
                        modalBody.innerHTML = '<p class="text-danger text-center p-5">failed to addload post. Please try again.</p>'; 
                    });
            }
        }
    });

    // =================================================================
    // SIMPLE SIDEBAR TOGGLE LOGIC
    // =================================================================
    function toggleSidebar(side) {
        const layout = document.querySelector('.home-layout');
        if (!layout) return;

        if (side === 'left') {
            layout.classList.toggle('left-collapsed');
        } else if (side === 'right') {
            layout.classList.toggle('right-collapsed');
        }
    }

    // Ensure the function is available globally
    window.toggleSidebar = toggleSidebar;

    // =================================================================
    // GROUP CHAT LOGIC
    // =================================================================
    
    const groupsListContainer = document.getElementById('userGroupsList');
    const chatModal = document.getElementById('groupChatModal');
    const chatMessagesContainer = document.getElementById('chatMessagesContainer');
    const chatForm = document.getElementById('chatForm');
    const chatInput = document.getElementById('chatInput');
    const currentGroupIdInput = document.getElementById('currentGroupId');
    const chatModalTitle = document.getElementById('chatModalTitle');

    // 1. Load Groups on Page Load
    if (groupsListContainer) {
        fetch('/Group/GetUserGroups')
            .then(res => res.json())
            .then(groups => {
                groupsListContainer.innerHTML = '';
                if (groups.length === 0) {
                    groupsListContainer.innerHTML = `
                        <div class="p-3 text-center text-muted bg-light rounded">
                            <i class="bi bi-chat-dots fs-4 d-block mb-2"></i>
                            <small>No active conversations</small>
                        </div>`;
                    return;
                }

                groups.forEach(g => {
                    const item = document.createElement('a');
                    item.href = '#';
                    item.className = 'list-group-item list-group-item-action border-0 rounded mb-1 d-flex align-items-center p-2';

                    const isDirectMessage = g.isDm || g.IsDm || g.isDirectMessage;
                    let iconHtml = '';

                    if (isDirectMessage) {
                        const pfpUrl = g.pfp || '/uploads/default_pfp.jpg';
                        iconHtml = `
                          <div class="chat-avatar-outline me-3">
                            <img src="${pfpUrl}" alt="">
                          </div>`;
                    } else {
                        // For group chats, use the group's ImageURL or default group icon
                        const groupPfp = g.imageURL || g.pfp || '/uploads/default_pfp.jpg';
                        iconHtml = `
                          <div class="chat-avatar-outline me-3">
                            ${groupPfp && groupPfp !== '/uploads/default_pfp.jpg' 
                              ? `<img src="${groupPfp}" alt="${g.name}" style="object-fit:cover;">` 
                              : '<div class="chat-icon"><i class="bi bi-people-fill"></i></div>'}
                          </div>`;
                    }

                    // Replace [SHARED_POST:xxx] with "Attachment"
                    let lastMessage = g.lastMessage || 'No messages yet';
                    if (lastMessage.startsWith('[SHARED_POST:') && lastMessage.endsWith(']')) {
                        lastMessage = 'Attachment';
                    }

                    item.innerHTML = `
                        ${iconHtml}
                        <div class="overflow-hidden">
                            <div class="fw-bold text-truncate">${g.name}</div>
                            <small class="text-muted text-truncate d-block">${lastMessage}</small>
                        </div>
                    `;
                    item.addEventListener('click', (e) => {
                        e.preventDefault();
                        openChatModal(g.id, g.name);
                    });
                    groupsListContainer.appendChild(item);
                });

    // Follow button flash effect
    document.querySelectorAll('.btn-follow-toggle').forEach(btn => {
        btn.addEventListener('click', () => {
            btn.classList.add('flash-pulse');
            // Let animation start before form submit (optional slight delay)
            setTimeout(() => {
                btn.closest('form').submit();
            }, 120);
        });
    });

            })
            .catch(err => console.error('Failed to load groups', err));
    }

    // 2. Open Chat Modal & Load Messages
    function openChatModal(groupId, groupName) {
        currentGroupIdInput.value = groupId;
        chatModalTitle.textContent = groupName;
        chatMessagesContainer.innerHTML = '<div class="text-center mt-4"><div class="spinner-border text-purple" role="status"></div></div>';
        
        const bsModal = new bootstrap.Modal(chatModal);
        bsModal.show();

        fetch(`/Group/GetGroupInfo?groupId=${groupId}`)
            .then(r => r.json())
            .then(groupData => {
            
                const subtitle = document.getElementById('groupHeaderSubtitle');      
                if (subtitle) {
                    subtitle.textContent = `${groupData.memberCount || 0} members`;
                }
                const groupInfoToggle_btn = document.getElementById("groupInfoToggle");
                groupInfoToggle_btn.style.display = groupData.isDirectMessage ? 'none' : 'block';
                groupInfoToggle_btn.display = groupData.isDirectMessage ? 'none' : 'block';
                
            })
            .catch(err => console.error('Error loading group info:', err));

        loadMessages(groupId);
    }

    function scrollChatToBottom(retries = 3) {
        if (!chatMessagesContainer) return;
        requestAnimationFrame(() => {
            chatMessagesContainer.scrollTop = chatMessagesContainer.scrollHeight;
            // Fallback: use last message
            const last = chatMessagesContainer.lastElementChild;
            if (last) last.scrollIntoView({ block: 'end' });
        });
        if (retries > 0) {
            setTimeout(() => scrollChatToBottom(retries - 1), 60); // retry after images/layout settle
        }
    }

    function loadMessages(groupId) {
        fetch(`/Group/GetMessages/${groupId}`)
            .then(res => res.json())
            .then(data => {
                chatMessagesContainer.innerHTML = '';

                const headerIconContainer = chatModal.querySelector('.modal-header .rounded-circle');
                        if (headerIconContainer) {
                            headerIconContainer.innerHTML = '';
                            headerIconContainer.style.backgroundImage = 'none';
                            headerIconContainer.style.backgroundSize = 'cover';
                            headerIconContainer.style.backgroundPosition = 'center';
                            headerIconContainer.style.backgroundRepeat = 'no-repeat';
                            headerIconContainer.style.borderRadius = '50%';
                            headerIconContainer.style.width = headerIconContainer.style.width || '48px';
                            headerIconContainer.style.height = headerIconContainer.style.height || '48px';
                            headerIconContainer.style.display = 'block';

                            if (data.isDm) {
                                // For DMs: use other user's pfp or fallback default
                                const imageUrl = data.headerPfp || '/uploads/default_pfp.jpg';
                                const sep = imageUrl.includes('?') ? '&' : '?';
                                headerIconContainer.style.backgroundImage = `url('${imageUrl}${sep}v=${Date.now()}')`;
                                
                            } else {
                                // For groups: use custom image if available, otherwise show icon
                                if (data.groupImageUrl) {
                                    const sep = data.groupImageUrl.includes('?') ? '&' : '?';
                                    headerIconContainer.style.backgroundImage = `url('${data.groupImageUrl}${sep}v=${Date.now()}')`;
                                    headerIconContainer.innerHTML = '';
                                    console.log('Background image set to:', headerIconContainer.style.backgroundImage);
                                    console.log('Data:', data);
                                } else {
                                    // Show fallback icon for groups without custom image
                                    headerIconContainer.innerHTML = '<div class="chat-icon"><i class="bi bi-people-fill"></i></div>';
                                    headerIconContainer.style.display = 'flex';
                                    headerIconContainer.style.alignItems = 'center';
                                    headerIconContainer.style.justifyContent = 'center';
                                    headerIconContainer.style.backgroundImage = 'none';
                                }
                            }
                        }
                
                if (data.messages.length === 0) {
                    chatMessagesContainer.innerHTML = '<div class="text-center text-muted mt-5">Start the conversation!</div>';
                    return;
                }

                data.messages.forEach(msg => {
                    const isMe = msg.isMe;
                    const div = document.createElement('div');
                    div.className = `d-flex mb-3 ${isMe ? 'justify-content-end' : 'justify-content-start'}`;

                    const senderName = msg.senderName; // Already checked in backend
                    const senderPfp = msg.senderPfp || '/uploads/default_pfp.jpg';

                    const pfpHtml = isMe ? '' : `
                        <a href="/Profile/Show/${encodeURIComponent(senderName)}" class="text-decoration-none ${senderName === 'Deleted User' ? 'pe-none' : ''}">
                             <img src="${senderPfp}" class="rounded-circle me-2 align-self-end" width="30" height="30" style="object-fit:cover;">
                        </a>`;

                    // Check if it's a shared post
                    if (msg.content === "Attachment") { // Check for our placeholder content
                        const post = msg.sharedPost;
                        
                        // Check if post was deleted
                        const isPostDeleted = !post;
                        
                        const mediaHtml = post?.videoURL ?
                            `<video src="${post.videoURL}" class="card-img-top chat-message-video" muted controls style="max-height: 200px; width: 100%; object-fit: cover;" preload="metadata">
                                <source src="${post.videoURL}" type="video/mp4">
                                <source src="${post.videoURL}" type="video/webm">
                                Your browser does not support the video tag.
                            </video>` :
                            (post?.imageURL ? `<img src="${post.imageURL}" class="card-img-top" alt="Shared post" style="max-height: 200px; object-fit: cover;">` : '');
                        
                        const postAuthorUsername = post?.username; // Already checked in backend
                        const postAuthorPfp = post?.userPfp || '/uploads/default_pfp.jpg';

                        // --- START: SHARED POST RENDERING (modified) ---
                        const postHtml = isPostDeleted ? `
                            <div class="shared-post-preview deleted-post" style="max-width: 300px; pointer-events: none; opacity: 0.8;">
                                <div class="card border shadow-sm">
                                    <div class="card-body p-3 text-center text-muted">
                                        <i class="bi bi-exclamation-triangle-fill"></i>
                                        <p class="small mb-0 mt-1">This post has been deleted.</p>
                                    </div>
                                </div>
                            </div>
                        ` : `
                            <div class="shared-post-preview" data-post-id="${post.id}" style="cursor: pointer; max-width: 300px;">
                                <div class="card border shadow-sm">
                                    ${mediaHtml}
                                    <div class="card-body p-2">
                                        <div class="d-flex align-items-center mb-2">
                                            <img src="${postAuthorPfp}"
                                                 alt="avatar" width="20" height="20" class="rounded-circle me-2" />
                                            <small class="fw-bold text-truncate">${postAuthorUsername}</small>
                                        </div>
                                        <p class="card-text small text-truncate">${post.content || ' '}</p>
                                    </div>
                                </div>
                            </div>
                        `;
                        // --- END: SHARED POST RENDERING (modified) ---

                        div.innerHTML = `
                            ${pfpHtml}
                            <div class="d-flex flex-column ${isMe ? 'align-items-end' : 'align-items-start'}" style="max-width:70%;">
                                ${!isMe ? `<small class="text-muted mb-1 fw-semibold" style="font-size:0.75rem;">${senderName}</small>` : ''}
                                ${postHtml}
                                <small class="text-muted mt-1" style="font-size:0.7rem;">${msg.sentAt}</small>
                            </div>
                        `;
                    } else {
                        // Regular message
                        div.innerHTML = `
                            ${pfpHtml}
                            <div class="d-flex flex-column ${isMe ? 'align-items-end' : 'align-items-start'}" style="max-width:70%;">
                                ${!isMe ? `<small class="text-muted mb-1 fw-semibold" style="font-size:0.75rem;">${senderName}</small>` : ''}
                                <div class="p-3 rounded-3 ${isMe ? 'msg-bubble-sent' : 'msg-bubble-received'}" style="word-wrap:break-word;box-shadow:0 1px 2px rgba(0,0,0,0.1);">
                                    ${msg.content}
                                </div>
                                <small class="text-muted mt-1" style="font-size:0.7rem;">${msg.sentAt}</small>
                            </div>
                        `;
                    }
                    
                    chatMessagesContainer.appendChild(div);
                });

                // Add click handlers for shared posts
                chatMessagesContainer.querySelectorAll('.shared-post-preview').forEach(preview => {
                    preview.addEventListener('click', function() {
                        const postId = this.dataset.postId;
                        
                        // Close chat modal first
                        const chatModalInstance = bootstrap.Modal.getInstance(chatModal);
                        if (chatModalInstance) {
                            chatModalInstance.hide();
                        }
                        
                        // Open post modal
                        const postModal = document.getElementById('postModal');
                        const postModalBody = document.getElementById('postModalBody');
                        
                        if (postModal && postModalBody) {
                            postModalBody.innerHTML = '<div class="d-flex justify-content-center p-5"><div class="spinner-border text-purple" role="status"></div></div>';
                            
                            const bsPostModal = new bootstrap.Modal(postModal);
                            bsPostModal.show();
                            
                            fetch(`/Posts/PostPartial/${postId}`)
                                .then(response => response.text())
                                .then(html => {
                                    postModalBody.innerHTML = html;
                                    
                                    // Ensure all videos in modal start muted
                                    const modalVideos = postModalBody.querySelectorAll('video');
                                    modalVideos.forEach(video => {
                                        video.muted = true;
                                    });
                                    
                                    setTimeout(initializeModalCommentsScroll, 100);
                                })
                                .catch(err => {
                                    console.error('Failed to load post:', err);
                                    postModalBody.innerHTML = '<p class="text-danger text-center p-5">Failed to load post.</p>';
                                });
                        }
                    });
                });

                // Force all chat message videos to stay muted
                chatMessagesContainer.querySelectorAll('.chat-message-video').forEach(video => {
                    video.muted = true;
                    
                    // Prevent unmuting
                    video.addEventListener('volumechange', function() {
                        if (!this.muted) {
                            this.muted = true;
                        }
                    });
                    
                    // Also prevent on play
                    video.addEventListener('play', function() {
                        this.muted = true;
                    });
                });

                // Scroll to bottom after rendering
                scrollChatToBottom();
            });
    }

    if (chatForm) {
        chatForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const content = chatInput.value.trim();
            const groupId = currentGroupIdInput.value;
            if (!content || !groupId) return;

            fetch('/Group/SendMessage', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `groupId=${groupId}&content=${encodeURIComponent(content)}`
            })
            .then(r => r.json())
            .then(d => {
                if (d.success) {
                    chatInput.value = '';
                    loadMessages(groupId);
                    // Extra final pass
                    setTimeout(() => scrollChatToBottom(), 120);
                }
            });
        });
    }

    // =================================================================
    // Notifications (REPLACE THIS ENTIRE SECTION)
    // =================================================================
    const notifBadge = document.getElementById('notifBadge');
    const notifList = document.getElementById('notifList');
    const notifMarkAll = document.getElementById('notifMarkAll');

    // Delegated click handler for all notification actions
    if (notifList) {
        notifList.addEventListener('click', function(e) {
            const readBtn = e.target.closest('.notif-read-btn');
            const followRequestBtn = e.target.closest('.follow-request-btn');

            if (readBtn) {
                e.preventDefault();
                const id = readBtn.dataset.id;
                fetch('/Notifications/MarkRead', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: `id=${id}`
                }).then(r => r.json()).then(d => {
                    if (d.success) fetchNotifications();
                });
            }

            if (followRequestBtn) {
                e.preventDefault();
                const actorUsername = followRequestBtn.dataset.actor;
                const action = followRequestBtn.dataset.action;

                fetch(`/Profile/HandleFollowRequest`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: `actorUsername=${encodeURIComponent(actorUsername)}&actionType=${action}`
                })
                .then(r => r.json())
                .then(d => {
                    if (d.success) {
                        fetchNotifications(); // Refresh notifications list
                    } else {
                        alert(d.message || 'An error occurred.');
                    }
                });
            }
        });
    }

    async function fetchNotifications() {
        if (!notifList) return;
        try {
            const res = await fetch('/Notifications/Unread');
            if (!res.ok) return;
            const data = await res.json();
            notifList.innerHTML = '';
            if (data.length === 0) {
                notifList.innerHTML = '<div class="p-3 text-muted small text-center">No new notifications</div>';
                notifBadge.classList.add('d-none');
                return;
            }
            notifBadge.textContent = data.length > 9 ? '9+' : data.length;
            notifBadge.classList.remove('d-none');

            data.forEach(n => {
                let text = '';
                let buttons = '';
                let isRequest = n.type === 'FollowRequest';

                // --- START: DELETED USER CHECK (NOTIFICATIONS) ---
                const isActorDeleted = !n.actor;
                const actorName = isActorDeleted ? 'Deleted User' : n.actor;
                const actorPfp = isActorDeleted ? '/uploads/default_pfp.jpg' : (n.actorPfp || '/uploads/default_pfp.jpg');
                // --- END: DELETED USER CHECK (NOTIFICATIONS) ---

                switch (n.type) {
                    case 'Like': text = `<strong>${actorName}</strong> liked your post.`; break;
                    case 'Comment': text = `<strong>${actorName}</strong> commented on your post.`; break;
                    case 'Follow': text = `<strong>${actorName}</strong> started following you.`; break;
                    case 'FollowRequest':
                        text = `<strong>${actorName}</strong> wants to follow you.`;
                        buttons = `
                            <div class="mt-2 d-flex gap-2">
                                <button class="btn btn-sm btn-primary flex-grow-1 follow-request-btn" data-actor="${actorName}" data-action="accept" ${isActorDeleted ? 'disabled' : ''}>Accept</button>
                                <button class="btn btn-sm btn-secondary flex-grow-1 follow-request-btn" data-actor="${actorName}" data-action="decline">Decline</button>
                            </div>`;
                        break;
                    default: text = 'New notification';
                }

                const item = document.createElement('div');
                item.className = 'list-group-item';
                
                const profileLink = isActorDeleted ? '#' : `/Profile/Show/${actorName}`;
                const postLink = n.postId ? `/Posts/Post/${n.postId}` : profileLink;

                const contentHtml = `
                    <div class="d-flex align-items-start gap-3">
                        <a href="${profileLink}" ${isActorDeleted ? 'style="pointer-events: none;"' : ''}><img src="${actorPfp}" class="rounded-circle" width="40" height="40" style="object-fit:cover;"></a>
                        <div class="flex-grow-1">
                            <div class="small notification-text">${text}</div>
                            ${buttons}
                            <div class="text-muted" style="font-size: 0.75rem; margin-top: 4px;">${new Date(n.time).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</div>
                        </div>
                        ${!isRequest ? `<button class="btn btn-sm btn-link text-muted p-0 notif-read-btn" data-id="${n.id}" title="Mark as read"><i class="bi bi-check-circle"></i></button>` : ''}
                    </div>
                `;

                if (isRequest || isActorDeleted) {
                    item.innerHTML = contentHtml;
                } else {
                    const link = document.createElement('a');
                    link.href = postLink;
                    link.className = 'text-decoration-none text-dark';
                    link.innerHTML = contentHtml;
                    item.appendChild(link);
                }
                notifList.appendChild(item);
            });
        } catch (err) {
            console.error('Notification fetch error:', err);
        }
    }

    if (notifMarkAll) {
        notifMarkAll.addEventListener('click', () => {
            fetch('/Notifications/MarkAll', { method: 'POST' })
                .then(r => r.json())
                .then(d => { if (d.success) fetchNotifications(); });
        });
    }

    fetchNotifications();
    setInterval(fetchNotifications, 30000);

    // =================================================================
    // Share Post Functionality
    // =================================================================
    let currentSharePostId = null;
    const selectedGroupsForShare = new Set();
    const shareModal = document.getElementById('shareModal');
    const shareSearchInput = document.getElementById('shareSearchInput');
    const shareGroupsList = document.getElementById('shareGroupsList');
    const selectedGroupsContainer = document.getElementById('selectedGroupsForShare');
    const confirmShareBtn = document.getElementById('confirmShareBtn');

    // Open share modal when clicking share button
    document.addEventListener('click', function(e) {
        const shareButton = e.target.closest('.share-button');
        if (shareButton) {
            e.preventDefault();
            currentSharePostId = shareButton.dataset.postId;
            selectedGroupsForShare.clear();
            if (selectedGroupsContainer) {
                selectedGroupsContainer.innerHTML = '';
            }
            
            // Show modal
            const bsModal = new bootstrap.Modal(shareModal);
            bsModal.show();
            
            // Load user's groups
            loadGroupsForShare();
        }
    });

    function loadGroupsForShare(query = '') {
        if (!shareGroupsList) return;
        
        shareGroupsList.innerHTML = '<div class="text-center text-muted py-3"><div class="spinner-border spinner-border-sm" role="status"></div></div>';
        
        const url = query ? `/Group/SearchGroups?q=${encodeURIComponent(query)}` : '/Group/GetUserGroups';
        
        fetch(url)
            .then(res => res.json())
            .then(groups => {
                shareGroupsList.innerHTML = '';
                
                if (groups.length === 0) {
                    shareGroupsList.innerHTML = '<div class="text-center text-muted py-3">No groups found</div>';
                    return;
                }
                
                groups.forEach(g => {
                    const item = document.createElement('div');
                    item.className = 'list-group-item list-group-item-action d-flex align-items-center justify-content-between';
                    item.style.cursor = 'pointer';
                    
                    const iconHtml = g.isDm && g.pfp
                        ? `<img src="${g.pfp}" class="rounded-circle me-2" width="32" height="32" style="object-fit:cover;">`
                        : '<i class="bi bi-people-fill fs-5 me-2 text-purple"></i>';
                    
                    const isSelected = selectedGroupsForShare.has(g.id);
                    
                    item.innerHTML = `
                        <div class="d-flex align-items-center">
                            ${iconHtml}
                            <span>${g.name}</span>
                        </div>
                        <i class="bi ${isSelected ? 'bi-check-circle-fill text-success' : 'bi-circle'}"></i>
                    `;
                    
                    item.addEventListener('click', () => toggleGroupSelection(g.id, g.name, item));
                    shareGroupsList.appendChild(item);
                });
            })
            .catch(err => {
                console.error('Failed to load groups:', err);
                shareGroupsList.innerHTML = '<div class="text-center text-danger py-3">Failed to load groups</div>';
            });
    }

    function toggleGroupSelection(groupId, groupName, element) {
        if (selectedGroupsForShare.has(groupId)) {
            selectedGroupsForShare.delete(groupId);
            element.querySelector('i').className = 'bi bi-circle';
        } else {
            selectedGroupsForShare.add(groupId);
            element.querySelector('i').className = 'bi bi-check-circle-fill text-success';
        }
        
        updateSelectedGroupsDisplay();
    }

    function updateSelectedGroupsDisplay() {
        if (!selectedGroupsContainer) return;
        
        if (selectedGroupsForShare.size === 0) {
            selectedGroupsContainer.innerHTML = '';
            return;
        }
        
        selectedGroupsContainer.innerHTML = `
            <div class="alert alert-info mb-0">
                <i class="bi bi-info-circle me-2"></i>
                ${selectedGroupsForShare.size} group${selectedGroupsForShare.size > 1 ? 's' : ''} selected
            </div>
        `;
    }

    // Search groups
    if (shareSearchInput) {
        let searchTimeout;
        shareSearchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            const query = this.value.trim();
            
            searchTimeout = setTimeout(() => {
                loadGroupsForShare(query);
            }, 300);
        });
    }

    // Confirm share
    if (confirmShareBtn) {
        confirmShareBtn.addEventListener('click', function() {
            if (selectedGroupsForShare.size === 0) {
                alert('Please select at least one group');
                return;
            }
            
            if (!currentSharePostId) return;
            
            confirmShareBtn.disabled = true;
            confirmShareBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Sharing...';
            
            fetch('/Group/SharePost', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify({
                    postId: parseInt(currentSharePostId),
                    groupIds: Array.from(selectedGroupsForShare)
                })
            })
            .then(res => {
                if (!res.ok) {
                    throw new Error('Network response was not ok');
                }
                return res.json();
            })
            .then(data => {
                if (data.success) {
                    // Check if any of the shared groups is currently open in chat
                    const currentOpenGroupId = currentGroupIdInput ? parseInt(currentGroupIdInput.value) : null;
                    const sharedGroupIds = Array.from(selectedGroupsForShare);
                    
                    // If the currently open chat is one of the groups we shared to, reload messages
                    if (currentOpenGroupId && sharedGroupIds.includes(currentOpenGroupId)) {
                        loadMessages(currentOpenGroupId);
                    }
                    
                    // Also refresh the groups list to update last message preview
                    if (groupsListContainer) {
                        fetch('/Group/GetUserGroups')
                            .then(res => res.json())
                            .then(groups => {
                                groupsListContainer.innerHTML = '';
                                if (groups.length === 0) {
                                    groupsListContainer.innerHTML = `
                                        <div class="p-3 text-center text-muted bg-light rounded">
                                            <i class="bi bi-chat-dots fs-4 d-block mb-2"></i>
                                            <small>No active conversations</small>
                                        </div>`;
                                    return;
                                }

                                groups.forEach(g => {
                                    const item = document.createElement('a');
                                    item.href = '#';
                                    item.className = 'list-group-item list-group-item-action border-0 rounded mb-1 d-flex align-items-center p-2';

                                    const isDirectMessage = g.isDm || g.IsDm || g.isDirectMessage;
                                    let iconHtml = '';

                                    if (isDirectMessage) {
                                        const pfpUrl = g.pfp || '/uploads/default_pfp.jpg';
                                        iconHtml = `
                                          <div class="chat-avatar-outline me-3">
                                            <img src="${pfpUrl}" alt="">
                                          </div>`;
                                    } else {
                                        const groupPfp = g.imageURL || g.pfp || '/uploads/default_pfp.jpg';
                                        iconHtml = `
                                          <div class="chat-avatar-outline me-3">
                                            ${groupPfp && groupPfp !== '/uploads/default_pfp.jpg' 
                                              ? `<img src="${groupPfp}" alt="${g.name}" style="object-fit:cover;">` 
                                              : '<div class="chat-icon"><i class="bi bi-people-fill"></i></div>'}
                                          </div>`;
                                    }

                                    let lastMessage = g.lastMessage || 'No messages yet';
                                    if (lastMessage.startsWith('[SHARED_POST:') && lastMessage.endsWith(']')) {
                                        lastMessage = 'Attachment';
                                    }

                                    item.innerHTML = `
                                        ${iconHtml}
                                        <div class="overflow-hidden">
                                            <div class="fw-bold text-truncate">${g.name}</div>
                                            <small class="text-muted text-truncate d-block">${lastMessage}</small>
                                        </div>
                                    `;
                                    item.addEventListener('click', (e) => {
                                        e.preventDefault();
                                        openChatModal(g.id, g.name);
                                    });
                                    groupsListContainer.appendChild(item);
                                });
                            })
                            .catch(err => console.error('Failed to reload groups', err));
                    }
                    
                    // Close modal
                    const bsModal = bootstrap.Modal.getInstance(shareModal);
                    bsModal.hide();
                    
                    // Show success message (you can customize this)
                    // alert('Post shared successfully!');
                    // ^ i don't think these alerts are very appealing
                    // Reset
                    selectedGroupsForShare.clear();
                    currentSharePostId = null;
                }
            })
            .catch(err => {
                console.error('Share failed:', err);
                alert('Failed to share post. Please try again.');
            })
            .finally(() => {
                confirmShareBtn.disabled = false;
                confirmShareBtn.innerHTML = 'Share';
            });
        });
    }

    // Reset modal on close
    if (shareModal) {
        shareModal.addEventListener('hidden.bs.modal', function() {
            selectedGroupsForShare.clear();
            currentSharePostId = null;
            if (shareSearchInput) shareSearchInput.value = '';
            if (selectedGroupsContainer) selectedGroupsContainer.innerHTML = '';
        });
    }

    // =================================================================
    // Comments Modal Logic
    // =================================================================
    const commentsModal = document.getElementById('commentsModal');
    const commentsModalOverlay = document.getElementById('commentsModalOverlay');
    const commentsModalBody = document.getElementById('commentsModalBody');
    const closeCommentsModalBtn = document.getElementById('closeCommentsModal');
    const modalCommentForm = document.getElementById('modalCommentForm');
    const modalPostIdInput = document.getElementById('modalPostId');
    
    let currentCommentsPage = 1;
    let isLoadingComments = false;
    let hasMoreComments = true;
    let currentPostId = null;

    // Open comments modal when clicking comment button
    document.addEventListener('click', function(e) {
        const commentsButton = e.target.closest('.comments-button');
        if (commentsButton) {
            e.preventDefault();
            const postId = commentsButton.dataset.postId;
            openCommentsModal(postId);
        }
    });

    // Close modal handlers
    closeCommentsModalBtn?.addEventListener('click', closeCommentsModal);
    commentsModalOverlay?.addEventListener('click', closeCommentsModal);

    // ESC key to close modal
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && commentsModal?.classList.contains('active')) {
            closeCommentsModal();
        }
    });

    function openCommentsModal(postId) {
        currentPostId = postId;
        currentCommentsPage = 1;
        hasMoreComments = true;
        
        // Set post ID in the form
        if (modalPostIdInput) {
            modalPostIdInput.value = postId;
        }
        
        // Show modal
        commentsModal?.classList.add('active');
        commentsModalOverlay?.classList.add('active');
        document.body.style.overflow = 'hidden';
        
        // Clear previous comments
        if (commentsModalBody) {
            commentsModalBody.innerHTML = '<div class="comments-loading"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>';
        }
        
        // Load comments
        loadCommentsForModal(postId);
    }

    function closeCommentsModal() {
        commentsModal?.classList.remove('active');
        commentsModalOverlay?.classList.remove('active');
        document.body.style.overflow = '';
        currentPostId = null;
        currentCommentsPage = 1;
        hasMoreComments = true;
        
        // Clear the form
        if (modalCommentForm) {
            modalCommentForm.reset();
        }
    }

    function loadCommentsForModal(postId, page = 1) {
        if (isLoadingComments) return;
        
        isLoadingComments = true;
        
        fetch(`/Comments/LoadComments?postId=${postId}&page=${page}&pageSize=10`, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(response => response.text())
        .then(html => {
            if (page === 1) {
                // First load - replace content
                if (html.trim().length === 0) {
                    commentsModalBody.innerHTML = '<div class="no-comments"><i class="bi bi-chat-dots"></i><p>No comments yet. Be the first to comment!</p></div>';
                    hasMoreComments = false;
                } else {
                    commentsModalBody.innerHTML = html;
                }
            } else {
                // Subsequent loads - append content
                if (html.trim().length === 0) {
                    hasMoreComments = false;
                } else {
                    commentsModalBody.insertAdjacentHTML('beforeend', html);
                }
            }
            isLoadingComments = false;
        })
        .catch(error => {
            console.error('Error loading comments:', error);
            if (page === 1) {
                commentsModalBody.innerHTML = '<div class="no-comments"><i class="bi bi-exclamation-triangle"></i><p>Failed to load comments. Please try again.</p></div>';
            }
            isLoadingComments = false;
        });
    }

    // Infinite scroll for comments
    if (commentsModalBody) {
        commentsModalBody.addEventListener('scroll', function() {
            if (isLoadingComments || !hasMoreComments || !currentPostId) return;
            
            const scrollTop = commentsModalBody.scrollTop;
            const scrollHeight = commentsModalBody.scrollHeight;
            const clientHeight = commentsModalBody.clientHeight;
            
            // Load more when scrolled near bottom
            if (scrollTop + clientHeight >= scrollHeight - 100) {
                currentCommentsPage++;
                loadCommentsForModal(currentPostId, currentCommentsPage);
            }
        });
    }

    // Handle comment submission from modal
    if (modalCommentForm) {
        modalCommentForm.addEventListener('submit', function(e) {
            e.preventDefault();
            const input = modalCommentForm.querySelector('input[name="commentText"]');
            const content = input.value.trim();
            const postId = modalPostIdInput.value;
            
            if (!content || !postId) return;
            
            fetch('/Comments/Add', {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/x-www-form-urlencoded', 
                    'X-Requested-With': 'XMLHttpRequest' 
                },
                body: `postId=${postId}&content=${encodeURIComponent(content)}`
            })
            .then(async response => {
                const text = await response.text();
                
                // Check if response is an error (starts with error indicator or is empty)
                if (!response.ok || text.includes('alert-danger') || text.includes('error')) {
                    // Try to parse JSON error message
                    try {
                        const errorData = JSON.parse(text);
                        throw new Error(errorData.message || 'Failed to add comment');
                    } catch (e) {
                        // If not JSON, check if HTML contains error message
                        const tempDiv = document.createElement('div');
                        tempDiv.innerHTML = text;
                        const errorMsg = tempDiv.querySelector('.alert-danger')?.textContent.trim();
                        throw new Error(errorMsg || 'Failed to add comment due to content validation');
                    }
                }
                
                return text;
            })
            .then(html => {
                // Remove "no comments" message if present
                const noComments = commentsModalBody.querySelector('.no-comments');
                if (noComments) {
                    noComments.remove();
                }
                
                // Add new comment at the bottom
                commentsModalBody.insertAdjacentHTML('beforeend', html);
                
                // Clear input
                input.value = '';
                
                // Scroll to bottom to show new comment
                commentsModalBody.scrollTop = commentsModalBody.scrollHeight;
                
                // Update comment count - search for all comment counts for this post
                const allCommentCounts = document.querySelectorAll('.comments-count');
                allCommentCounts.forEach(span => {
                    const postContainer = span.closest(`#post-${postId}`) || 
                                        span.closest(`#post-modal-${postId}`) ||
                                        span.closest(`[id="post-${postId}"]`) ||
                                        span.closest(`[data-post-id="${postId}"]`);
                    
                    if (postContainer) {
                        // Extract current count from text (handles both "X comments" and badge with icon)
                        const textContent = span.textContent.trim();
                        const matches = textContent.match(/\d+/);
                        const currentCount = matches ? parseInt(matches[0]) : 0;
                        const newCount = currentCount + 1;
                        
                        // Check if this is a badge (profile grid) or regular text
                        if (span.classList.contains('badge')) {
                            // Badge format - just number with icon
                            span.innerHTML = `<i class="bi bi-chat-fill"></i> ${newCount}`;
                        } else {
                            // Regular format - "X comments"
                            span.textContent = `${newCount} comment${newCount !== 1 ? 's' : ''}`;
                        }
                    }
                });
                
                // Reload comments preview in feed (with slight delay to ensure comment is added)
                setTimeout(() => {
                    refreshCommentsPreview(postId);
                }, 100);
            })
            .catch(error => {
                console.error('Error adding comment:', error);
                
                // Create styled error message
                const errorDiv = document.createElement('div');
                errorDiv.className = 'alert alert-danger alert-dismissible fade show mt-2';
                errorDiv.style.cssText = 'animation: slideIn 0.3s ease-out; margin: 0 1rem;';
                errorDiv.innerHTML = `
                    <i class="bi bi-exclamation-triangle-fill me-2"></i>
                    <strong>Error:</strong> ${error.message || 'Failed to add comment. Please check your content.'}
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                `;
                
                // Remove any existing error messages
                const existingError = modalCommentForm.parentElement.querySelector('.alert-danger');
                if (existingError) existingError.remove();
                
                // Insert error message after the form
                modalCommentForm.parentElement.insertBefore(errorDiv, modalCommentForm.nextSibling);
                
                // Auto-remove after 5 seconds
                setTimeout(() => {
                    errorDiv.classList.remove('show');
                    setTimeout(() => errorDiv.remove(), 150);
                }, 5000);
            });
        });
    }
    // =================================================================
    // FOLLOWERS/FOLLOWING MODAL LOGIC
    // =================================================================
    
let currentFollowPage = 1;
let currentFollowType = '';
let currentUsername = '';
let isLoadingFollows = false;
let hasMoreFollows = true;
let followScrollHandler = null;

// Initialize followers/following click handlers (REMOVE the DOMContentLoaded wrapper here)
document.querySelectorAll('.followers-link').forEach(link => {
    link.addEventListener('click', function() {
        openFollowModal('followers', this.dataset.username);
    });
});

document.querySelectorAll('.following-link').forEach(link => {
    link.addEventListener('click', function() {
        openFollowModal('following', this.dataset.username);
    });
});
    function openFollowModal(type, username) {
        currentFollowType = type;
        currentUsername = username;
        currentFollowPage = 1;
        hasMoreFollows = true;
        isLoadingFollows = false;

        const modal = document.getElementById('followModal');
        if (!modal) return;

        const modalLabel = document.getElementById('followModalLabel');
        const modalBody = document.getElementById('followModalBody');

        // Set title
        if (type === 'followers') {
            modalLabel.textContent = 'Followers';
        } else if (type === 'following') {
            modalLabel.textContent = 'Following';
        } else if (type === 'notFollowingBack') {
            modalLabel.textContent = 'Not Following Back';
        }

        // Reset body content and remove any prior scroll handler
        if (followScrollHandler) {
            modalBody.removeEventListener('scroll', followScrollHandler);
            followScrollHandler = null;
        }

        // Show loading container that holds the list
        const isOwnProfile = modal.dataset.ownProfile === 'true';
        if (type === 'following' && isOwnProfile) {
            modalBody.innerHTML = `
                <button class="btn btn-outline-primary w-100 mb-3" id="notFollowingBackBtn">
                    <i class="bi bi-person-x"></i> Not Following Back
                </button>
                <div id="followList" style="max-height: 400px; overflow-y: auto;"></div>
            `;
            const btn = document.getElementById('notFollowingBackBtn');
            btn?.addEventListener('click', function() {
                openFollowModal('notFollowingBack', currentUsername);
            });
        } else {
            modalBody.innerHTML = '<div id="followList" style="max-height: 400px; overflow-y: auto;"></div>';
        }

        // Load first page
        loadFollows();

        // Show modal (fresh instance each time)
        let bsModal = bootstrap.Modal.getInstance(modal);
        if (!bsModal) {
            bsModal = new bootstrap.Modal(modal);
        }
        bsModal.show();

        // Attach a single scroll listener to modalBody (or followList if you prefer)
        followScrollHandler = function() {
            if (isLoadingFollows || !hasMoreFollows) return;
            const container = document.getElementById('followList') || modalBody;
            const scrollTop = container.scrollTop;
            const scrollHeight = container.scrollHeight;
            const clientHeight = container.clientHeight;

            if (scrollTop + clientHeight >= scrollHeight - 100) {
                currentFollowPage++;
                loadFollows();
            }
        };
        // Listen on followList to avoid modal header affecting scroll math
        const listContainer = document.getElementById('followList') || modalBody;
        listContainer.addEventListener('scroll', followScrollHandler);

        // Clean up on hide
        modal.addEventListener('hidden.bs.modal', function onHide() {
            // remove listener
            const list = document.getElementById('followList') || modalBody;
            if (followScrollHandler) {
                list.removeEventListener('scroll', followScrollHandler);
                followScrollHandler = null;
            }
            // reset state
            currentFollowPage = 1;
            hasMoreFollows = true;
            isLoadingFollows = false;
            currentFollowType = '';
            currentUsername = '';
            // clear content
            modalBody.innerHTML = '<div class="text-center py-3"><div class="spinner-border text-purple" role="status"></div></div>';
            // remove this one-off handler attachment
            modal.removeEventListener('hidden.bs.modal', onHide);
        });
    }

    async function loadFollows() {
        if (isLoadingFollows) return;
        isLoadingFollows = true;

        let endpoint = '';
        if (currentFollowType === 'followers') {
            endpoint = `/Profile/GetFollowers/${currentUsername}?page=${currentFollowPage}`;
        } else if (currentFollowType === 'following') {
            endpoint = `/Profile/GetFollowing/${currentUsername}?page=${currentFollowPage}`;
        } else if (currentFollowType === 'notFollowingBack') {
            endpoint = `/Profile/GetNotFollowingBack/${currentUsername}?page=${currentFollowPage}`;
        } else {
            isLoadingFollows = false;
            return;
        }

        try {
            const res = await fetch(endpoint);
            const data = await res.json();

            const targetContainer = document.getElementById('followList');
            if (!targetContainer) return;

            if (currentFollowPage === 1) {
                targetContainer.innerHTML = '';
            }

            if (!data.users || data.users.length === 0) {
                hasMoreFollows = false;
                if (currentFollowPage === 1) {
                    targetContainer.innerHTML = '<div class="text-center text-muted py-3">No users found</div>';
                }
                return;
            }

            data.users.forEach(user => {
                const userItem = document.createElement('div');
                userItem.className = 'd-flex align-items-center p-3 border-bottom';
                userItem.innerHTML = `
                    <a href="/Profile/Show/${user.userName}" class="text-decoration-none d-flex align-items-center flex-grow-1">
                        <img src="${user.pfpURL || '/uploads/default_pfp.jpg'}" 
                             alt="${user.userName}" 
                             class="rounded-circle me-3" 
                             width="40" 
                             height="40"
                             style="object-fit: cover;">
                        <div>
                            <div class="fw-bold text-dark">${user.userName}</div>
                            <div class="text-muted small">${user.firstName ?? ''} ${user.lastName ?? ''}</div>
                        </div>
                    </a>
                `;
                targetContainer.appendChild(userItem);
            });

            // If less than a typical page, stop further loads
            if (data.users.length < 20) {
                hasMoreFollows = false;
            }
        } catch (err) {
            console.error('Failed to load follows:', err);
        } finally {
            isLoadingFollows = false;
        }
    }

    // Video play/pause toggle on feed
    document.addEventListener('click', function(e) {
        const video = e.target.closest('.feed-video');
        if (video) {
            e.preventDefault();
            if (video.paused) {
                video.play();
            } else {
                video.pause();
            }
        }
    });

    // Intersection Observer for autoplay videos when in viewport
    if ('IntersectionObserver' in window) {
        const videoObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                const video = entry.target;
                if (entry.isIntersecting) {
                    video.play().catch(err => console.log('Autoplay prevented:', err));
                } else {
                    video.pause();
                }
            });
        }, {
            threshold: 0.5 // Video plays when 50% visible
        });

        // Observe all feed videos
        document.querySelectorAll('.feed-video').forEach(video => {
            videoObserver.observe(video);
        });
    }

    // Panel member search (inside openPanel, after rendering members list)
    const input_panel = document.querySelector('#searchInput_panel');
    const results_panel = document.querySelector('#searchResults_panel');
    const selectedMembersPanel = document.querySelector('#selectedMembers_panel');
    const addMembersBtn_panel = document.querySelector('#addMembersBtn_panel');
    const selectedUsersPanel = new Set();

    if (input_panel && results_panel && selectedMembersPanel) {
        input_panel.addEventListener('input', async () => {
            const q = input_panel.value.trim();
            if (!q) { results_panel.style.display = 'none'; return; }

            try {
                const res = await fetch(`/Search/LiveChat?q=${encodeURIComponent(q)}`);
                if (!res.ok) { results_panel.style.display = 'none'; return; }
                const html = await res.text();
                results_panel.innerHTML = html;
                results_panel.style.display = 'block';
            } catch (err) {
                console.error('Search error:', err);
                results_panel.style.display = 'none';
            }
        });

        results_panel.addEventListener('click', (e) => {
            const item = e.target.closest('li');
            if (!item) return;

            const userId = item.dataset.userId;
            const username = item.dataset.username;
            const pfp = item.dataset.pfp || '/uploads/default_pfp.jpg';

            // Skip if already selected
            if (selectedUsersPanel.has(userId)) {
                input_panel.value = '';
                results_panel.style.display = 'none';
                return;
            }

            selectedUsersPanel.add(userId);

            const chip = document.createElement('div');
            chip.className = 'user-chip';
            chip.style.cssText = 'display:inline-flex; align-items:center; gap:8px; padding:6px 10px; background:#f3efff; color:#3b0b8b; border-radius:999px; font-size:0.85rem; border:1px solid rgba(111,66,193,0.12);';
            chip.innerHTML = `
                <img src="${pfp}" alt="" style="width:20px; height:20px; object-fit:cover; border-radius:50%;">
                <span>${username}</span>
                <input type="hidden" name="newMemberIds" value="${userId}" />
                <button type="button" class="btn-close ms-2" style="width:0.5em; height:0.5em;" aria-label="Remove"></button>
            `;

            chip.querySelector('.btn-close').addEventListener('click', () => {
                chip.remove();
                selectedUsersPanel.delete(userId);
            });

            selectedMembersPanel.appendChild(chip);
            input_panel.value = '';
            results_panel.style.display = 'none';
        });

        // Close results when clicking outside
        document.addEventListener('click', (e) => {
            if (e.target !== input_panel && !results_panel.contains(e.target)) {
                results_panel.style.display = 'none';
            }
        });
    }

    // Add Members button submission
    if (addMembersBtn_panel) {
        addMembersBtn_panel.addEventListener('click', () => {
            const gid = document.getElementById('currentGroupId')?.value;
            if (!gid || selectedUsersPanel.size === 0) {
                console.warn('Select at least one member');
                return;
            }

            const userIds = Array.from(selectedUsersPanel);

            // Reuse existing form or create one
            let form = document.getElementById('addMembersForm');
            if (!form) {
                form = document.createElement('form');
                form.id = 'addMembersForm';
                form.method = 'POST';
                form.action = '/Group/AddMember';
                form.style.display = 'none';
                document.body.appendChild(form);
            }

            // Clear previous inputs
            form.innerHTML = '';

            // groupId input
            const gidInput = document.createElement('input');
            gidInput.type = 'hidden';
            gidInput.name = 'groupId';
            gidInput.value = gid;
            form.appendChild(gidInput);

    
            // userIds (repeatable)
            userIds.forEach(id => {
                const userInput = document.createElement('input');
                userInput.type = 'hidden';
                userInput.name = 'userIds';
                userInput.value = id;
                form.appendChild(userInput);
            });

            form.submit();
        });
    }

    // =================================================================
    // SUGGESTED USERS LOGIC
    // =================================================================
    const suggestedUsersList = document.getElementById('suggestedUsersList');
    
    if (suggestedUsersList) {
        fetch('/Profile/GetSuggestedUsers')
            .then(res => res.json())
            .then(data => {
                suggestedUsersList.innerHTML = '';
                
                if (data.users.length === 0) {
                    suggestedUsersList.innerHTML = `
                        <div class="p-3 text-center text-muted">
                            <i class="bi bi-person-plus fs-4 d-block mb-2"></i>
                            <small>No suggestions yet</small>
                        </div>`;
                    return;
                }
                
                data.users.forEach(user => {
                    const userItem = document.createElement('a');
                    userItem.href = `/Profile/Show/${user.userName}`;
                    userItem.className = 'd-flex align-items-center mb-3 text-decoration-none text-dark';
                    userItem.style.cursor = 'pointer';
                    
                    const pfpUrl = user.pfpURL || '/uploads/default_pfp.jpg';
                    const mutualText = user.mutualFollowers === 1 
                        ? '1 mutual follower' 
                        : `${user.mutualFollowers} mutual followers`;
                    
                    userItem.innerHTML = `
                        <img src="${pfpUrl}" alt="${user.userName}" 
                             class="rounded-circle me-2" 
                             style="width: 40px; height: 40px; object-fit: cover;">
                        <div class="flex-grow-1">
                            <div class="fw-bold small">${user.userName}</div>
                            <div class="text-muted small">${mutualText}</div>
                        </div>
                    `;
                    
                    suggestedUsersList.appendChild(userItem);
                });
            })
            .catch(err => {
                console.error('Failed to load suggested users:', err);
                suggestedUsersList.innerHTML = `
                    <div class="p-3 text-center text-muted">
                        <small>Failed to load suggestions</small>
                    </div>`;
            });
    }

    // =================================================================
    // VIDEO THUMBNAIL LOADER FOR PROFILE GRID
    // =================================================================
    const videoThumbnails = document.querySelectorAll('.video-thumbnail');
    videoThumbnails.forEach(video => {
        video.addEventListener('loadeddata', function() {
            // Seek to 1 second to get a better frame
            this.currentTime = 1;
        });
        
        video.addEventListener('seeked', function() {
            // Once seeked, the frame is displayed
            this.pause();
        });
        
        // Start loading
        video.load();
    });
    
    // =================================================================
    // STOP ALL VIDEOS WHEN ANY MODAL CLOSES
    // =================================================================
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('hidden.bs.modal', function() {
            // Stop all videos in this modal
            const videos = this.querySelectorAll('video');
            videos.forEach(video => {
                video.pause();
                video.currentTime = 0;
            });
        });
    });
});

// --- START: delegated click handler guard (add near other event handlers or bottom of file) ---
document.addEventListener('click', function (ev) {
    const preview = ev.target.closest('.shared-post-preview');
    if (!preview) return;

    const postId = preview.dataset.postId;
    // If there is no postId (deleted post), do nothing.
    if (!postId) {
        ev.stopPropagation();
        ev.preventDefault();
        return;
    }

    // existing logic to open the post modal (run only when postId exists)
    // Example: fetch post html and show modal
    // NOTE: if you already have a handler, replace its body with this guard + existing code
    fetch(`/Posts/GetPostPartial/${encodeURIComponent(postId)}`)
        .then(r => r.text())
        .then(html => {
            const modalBody = document.getElementById('postModalBody');
            if (modalBody) {
                modalBody.innerHTML = html;
                const postModal = new bootstrap.Modal(document.getElementById('postModal'));
                postModal.show();
            }
        })
        .catch(() => {
            // optionally show a small toast / silent fail
        });
}, false);
// --- END: delegated click handler guard ---
