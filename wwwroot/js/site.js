// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

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

                if (isAuthorized) {
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

                        el.innerHTML = leftHtml + checkboxHtml;
                        memberListContainer.appendChild(el);
                    });

                } else {
                    // --- RENDER SIMPLE MEMBER LIST (for non-moderators) ---
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
                    document.querySelectorAll(`#post-${postId} .likes-count, #post-modal-${postId} .likes-count`).forEach(span => {
                        span.textContent = `${data.likesCount} likes`;
                    });
                }
            }).catch(error => console.error('Error toggling like:', error));
        }

        // --- Handle Edit Button Click (to show the form) ---
        if (editButton) {
            e.preventDefault();
            const commentId = editButton.dataset.commentId;
            const displayDiv = document.getElementById(`comment-display-${commentId}`);
            const editForm = document.querySelector(`.edit-comment-form[data-comment-id='${commentId}']`);
            
            if (displayDiv && editForm) {
                displayDiv.style.display = 'none';
                editForm.style.display = 'block';
                editForm.querySelector('input[name="content"]').focus();
            }
        }

        // --- Handle Delete Button Click ---
        if (deleteButton) {
            e.preventDefault();
            const commentId = deleteButton.dataset.commentId;
            
            if (confirm('Are you sure you want to delete this comment?')) {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                
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
                        const container = document.getElementById(`comment-container-${commentId}`);
                        if (container) {
                            container.style.transition = 'opacity 0.3s';
                            container.style.opacity = '0';
                            setTimeout(() => container.remove(), 300);
                        }
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
        if (e.target.matches('.add-comment-form')) {
            e.preventDefault();
            const form = e.target;
            const postId = form.dataset.postId;
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
            }).then(response => response.text()).then(html => {
                const commentsContainer = 
                    document.getElementById(`comments-list-${postId}`) ||
                    document.getElementById(`comments-for-modal-${postId}`);
                
                if (commentsContainer) {
                    commentsContainer.insertAdjacentHTML('beforeend', html);
                }
                input.value = '';
            }).catch(error => console.error('Error adding comment:', error));
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
                    const contentSpan = document.getElementById(`comment-content-${commentId}`);
                    const displayDiv = document.getElementById(`comment-display-${commentId}`);
                    
                    if (contentSpan) contentSpan.textContent = data.content;
                    if (displayDiv) displayDiv.style.display = 'flex';
                    form.style.display = 'none';
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
    
    // Also handle clicks on post images in feed (event delegation)
    document.addEventListener('click', function(e) {
        const postImageLink = e.target.closest('.post-image-link');
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
                        setTimeout(initializeModalCommentsScroll, 100);
                    })
                    .catch(err => { 
                        console.error('Modal load error:', err);
                        modalBody.innerHTML = '<p class="text-danger text-center p-5">Failed to load post. Please try again.</p>'; 
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

                    const pfpHtml = isMe ? '' : `
                        <a href="/Profile/Show/${encodeURIComponent(msg.senderName)}" class="text-decoration-none">
                            <img src="${msg.senderPfp || '/uploads/default_pfp.jpg'}"
                                 class="rounded-circle me-2 align-self-end"
                                 width="30" height="30"
                                 style="object-fit:cover;">
                        </a>`;

                    // Check if it's a shared post
                    if (msg.sharedPost) {
                        const post = msg.sharedPost;
                        const postHtml = `
                            <div class="shared-post-preview" data-post-id="${post.id}" style="cursor: pointer; max-width: 300px;">
                                <div class="card border shadow-sm">
                                    ${post.imageURL ? `<img src="${post.imageURL}" class="card-img-top" alt="Shared post" style="max-height: 200px; object-fit: cover;">` : ''}
                                    <div class="card-body p-2">
                                        <div class="d-flex align-items-center mb-2">
                                            <img src="${post.userPfp || '/uploads/default_pfp.jpg'}"
                                                 alt="avatar" width="20" height="20" class="rounded-circle me-2" />
                                            <small class="fw-bold text-truncate">${post.username}</small>
                                        </div>
                                        ${post.content ? `<p class="card-text small text-muted mb-1" style="display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;">${post.content}</p>` : ''}
                                        <div class="d-flex gap-2 text-muted" style="font-size: 0.7rem;">
                                            <span><i class="bi bi-heart-fill text-danger"></i> ${post.likesCount}</span>
                                            <span><i class="bi bi-chat-fill"></i> ${post.commentsCount}</span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        `;
                        
                        div.innerHTML = `
                            ${pfpHtml}
                            <div class="d-flex flex-column ${isMe ? 'align-items-end' : 'align-items-start'}" style="max-width:70%;">
                                ${!isMe ? `<small class="text-muted mb-1 fw-semibold" style="font-size:0.75rem;">${msg.senderName}</small>` : ''}
                                ${postHtml}
                                <small class="text-muted mt-1" style="font-size:0.7rem;">${msg.sentAt}</small>
                            </div>
                        `;
                    } else {
                        // Regular message
                        div.innerHTML = `
                            ${pfpHtml}
                            <div class="d-flex flex-column ${isMe ? 'align-items-end' : 'align-items-start'}" style="max-width:70%;">
                                ${!isMe ? `<small class="text-muted mb-1 fw-semibold" style="font-size:0.75rem;">${msg.senderName}</small>` : ''}
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
                                    setTimeout(initializeModalCommentsScroll, 100);
                                })
                                .catch(err => {
                                    console.error('Failed to load post:', err);
                                    postModalBody.innerHTML = '<p class="text-danger text-center p-5">Failed to load post.</p>';
                                });
                        }
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

                switch (n.type) {
                    case 'Like': text = `<strong>${n.actor}</strong> liked your post.`; break;
                    case 'Comment': text = `<strong>${n.actor}</strong> commented on your post.`; break;
                    case 'Follow': text = `<strong>${n.actor}</strong> started following you.`; break;
                    case 'FollowRequest':
                        text = `<strong>${n.actor}</strong> wants to follow you.`;
                        buttons = `
                            <div class="mt-2 d-flex gap-2">
                                <button class="btn btn-sm btn-primary flex-grow-1 follow-request-btn" data-actor="${n.actor}" data-action="accept">Accept</button>
                                <button class="btn btn-sm btn-secondary flex-grow-1 follow-request-btn" data-actor="${n.actor}" data-action="decline">Decline</button>
                            </div>`;
                        break;
                    default: text = 'New notification';
                }

                const item = document.createElement('div');
                item.className = 'list-group-item';
                
                // Use a link for clickable notifications, but not for requests with buttons
                const contentHtml = `
                    <div class="d-flex align-items-start gap-3">
                        <a href="/Profile/Show/${n.actor}"><img src="${n.actorPfp || '/uploads/default_pfp.jpg'}" class="rounded-circle" width="40" height="40" style="object-fit:cover;"></a>
                        <div class="flex-grow-1">
                            <div class="small notification-text">${text}</div>
                            ${buttons}
                            <div class="text-muted" style="font-size: 0.75rem; margin-top: 4px;">${new Date(n.time).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</div>
                        </div>
                        ${!isRequest ? `<button class="btn btn-sm btn-link text-muted p-0 notif-read-btn" data-id="${n.id}" title="Mark as read"><i class="bi bi-check-circle"></i></button>` : ''}
                    </div>
                `;

                if (isRequest) {
                    item.innerHTML = contentHtml;
                } else {
                    const link = document.createElement('a');
                    link.href = n.postId ? `/Posts/Post/${n.postId}` : `/Profile/Show/${n.actor}`;
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
                    // Close modal
                    const bsModal = bootstrap.Modal.getInstance(shareModal);
                    bsModal.hide();
                    
                    // Show success message (you can customize this)
                    alert('Post shared successfully!');
                    
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
            .then(response => response.text())
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
                
                // Update comment count on the button
                const commentButton = document.querySelector(`.comments-button[data-post-id="${postId}"]`);
                if (commentButton) {
                    const countSpan = commentButton.querySelector('.comments-count');
                    if (countSpan) {
                        const currentCount = parseInt(countSpan.textContent) || 0;
                        countSpan.textContent = `${currentCount + 1} comments`;
                    }
                }
            })
            .catch(error => console.error('Error adding comment:', error));
        });
    }
});
    // =================================================================
    // FOLLOWERS/FOLLOWING MODAL LOGIC
    // =================================================================
    
    let currentFollowPage = 1;
    let currentFollowType = '';
    let currentUsername = '';
    let isLoadingFollows = false;
    let hasMoreFollows = true;

    // Initialize followers/following click handlers
    document.addEventListener('DOMContentLoaded', function() {
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
    });

    function openFollowModal(type, username) {
        currentFollowType = type;
        currentUsername = username;
        currentFollowPage = 1;
        hasMoreFollows = true;

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

        // Show loading
        modalBody.innerHTML = '<div class="text-center py-3"><div class="spinner-border text-purple" role="status"></div></div>';

        // Add "Not Following Back" button if viewing following on own profile
        const isOwnProfile = modal.dataset.ownProfile === 'true';
        if (type === 'following' && isOwnProfile) {
            modalBody.innerHTML = `
                <button class="btn btn-outline-primary w-100 mb-3" id="notFollowingBackBtn">
                    <i class="bi bi-person-x"></i> Not Following Back
                </button>
                <div id="followList"></div>
            `;
            
            setTimeout(() => {
                const btn = document.getElementById('notFollowingBackBtn');
                if (btn) {
                    btn.addEventListener('click', function() {
                        openFollowModal('notFollowingBack', currentUsername);
                    });
                }
            }, 100);
        } else {
            modalBody.innerHTML = '<div id="followList"></div>';
        }

        // Load first page
        loadFollows();

        // Show modal or keep it open if already visible
        let bsModal = bootstrap.Modal.getInstance(modal);
        if (!bsModal) {
            bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        }

        // Remove existing scroll listeners to prevent duplicates
        const newModalBody = modalBody.cloneNode(true);
        modalBody.parentNode.replaceChild(newModalBody, modalBody);
        
        // Setup scroll listener on the new element
        newModalBody.addEventListener('scroll', function() {
            if (isLoadingFollows || !hasMoreFollows) return;
            
            const scrollTop = newModalBody.scrollTop;
            const scrollHeight = newModalBody.scrollHeight;
            const clientHeight = newModalBody.clientHeight;

            if (scrollTop + clientHeight >= scrollHeight - 100) {
                currentFollowPage++;
                loadFollows();
            }
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
        }

        try {
            const res = await fetch(endpoint);
            const data = await res.json();

            const targetContainer = document.getElementById('followList') || document.getElementById('followModalBody');

            if (currentFollowPage === 1) {
                targetContainer.innerHTML = '';
            }

            if (data.users.length === 0) {
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
                            <div class="text-muted small">${user.firstName} ${user.lastName}</div>
                        </div>
                    </a>
                `;
                targetContainer.appendChild(userItem);
            });

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

